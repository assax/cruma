import { afterEach, beforeEach, describe, expect, it } from "vitest";
import type { Editor } from "@tiptap/core";
import { TextSelection } from "@tiptap/pm/state";
import { createCrumaEditor, getBlocks, isAllowedLink, setBlocks } from "../src/createEditor";
import type { JsonNode } from "../src/schema";
import fixture from "../../../../tests/Cruma.Content.Tests/Fixtures/document-v1-all-elements.json";

// jsdom nemá ClipboardEvent, který ProseMirror používá pro simulaci vložení.
if (typeof globalThis.ClipboardEvent === "undefined") {
  (globalThis as Record<string, unknown>)["ClipboardEvent"] = class extends Event {
    clipboardData: DataTransfer | null = null;
  };
}

let counter = 0;
const generateId = () => `new-${++counter}`;

const paragraph = (id: string | null, text: string): JsonNode => ({
  type: "paragraph",
  attrs: id ? { id } : {},
  content: text ? [{ type: "text", text }] : [],
});

let editor: Editor;

function open(blocks: JsonNode[]): Editor {
  editor = createCrumaEditor({ element: document.createElement("div"), blocks, generateId });
  return editor;
}

const ids = () => getBlocks(editor).map((block) => block.attrs?.["id"]);
const texts = () => getBlocks(editor).map((block) => (block.content ?? []).map((child) => child.text ?? "").join(""));

/** Pozice na začátku textu bloku nejvyšší úrovně s indexem. */
function startOfBlock(index: number): number {
  let position = 0;
  editor.state.doc.forEach((node, offset, i) => {
    if (i === index) {
      position = offset + 1;
    }
  });
  return position;
}

beforeEach(() => {
  counter = 0;
});

afterEach(() => editor?.destroy());

describe("identifikátory bloků (FR-7 akc. 5, CNT-002)", () => {
  it("úprava textu identifikátor nemění", () => {
    open([paragraph("a", "Ahoj"), paragraph("b", "Světe")]);

    editor.chain().setTextSelection(startOfBlock(0) + 4).insertContent(" všem").run();

    expect(ids()).toEqual(["a", "b"]);
    expect(texts()).toEqual(["Ahoj všem", "Světe"]);
  });

  it("změna typu bloku identifikátor ponechá", () => {
    open([paragraph("a", "Nadpis"), paragraph("b", "Text")]);

    editor.chain().setTextSelection(startOfBlock(0) + 1).toggleHeading({ level: 2 }).run();

    expect(getBlocks(editor)[0].type).toBe("heading");
    expect(ids()).toEqual(["a", "b"]);
  });

  it("zabalení do seznamu ponechá identifikátor bloku", () => {
    open([paragraph("a", "položka"), paragraph("b", "Text")]);

    editor.chain().setTextSelection(startOfBlock(0) + 1).toggleBulletList().run();

    const blocks = getBlocks(editor);
    expect(blocks[0].type).toBe("bulletList");
    expect(ids()).toEqual(["a", "b"]);
    expect(JSON.stringify(blocks[0].content)).not.toContain('"id":"');
  });

  it("rozdělení ponechá identifikátor první části, druhá dostane nový", () => {
    open([paragraph("a", "PrvníDruhá")]);

    editor.chain().setTextSelection(startOfBlock(0) + 5).splitBlock().run();

    expect(texts()).toEqual(["První", "Druhá"]);
    expect(ids()).toEqual(["a", "new-1"]);
  });

  it("spojení ponechá identifikátor prvního bloku", () => {
    open([paragraph("a", "První"), paragraph("b", "Druhá")]);

    editor.chain().setTextSelection(startOfBlock(1)).joinBackward().run();

    expect(texts()).toEqual(["PrvníDruhá"]);
    expect(ids()).toEqual(["a"]);
  });

  it("přesun bloku identifikátor ponechá", () => {
    open([paragraph("a", "A"), paragraph("b", "B"), paragraph("c", "C")]);
    const node = editor.state.doc.child(0);

    editor.view.dispatch(editor.state.tr.delete(0, node.nodeSize).insert(editor.state.doc.content.size - node.nodeSize, node));

    expect(ids()).toEqual(["b", "c", "a"]);
  });

  it("duplikovaný blok dostane nový identifikátor, původní si svůj ponechá", () => {
    open([paragraph("a", "A"), paragraph("b", "B")]);
    const copy = editor.state.doc.child(1);

    editor.view.dispatch(editor.state.tr.insert(0, copy));

    expect(texts()).toEqual(["B", "A", "B"]);
    expect(ids()).toEqual(["new-1", "a", "b"]);
  });

  it("vložený obsah ze schránky dostane nové identifikátory", () => {
    open([paragraph("a", "A")]);

    editor.chain().setTextSelection(startOfBlock(0) + 1).run();
    editor.view.pasteHTML('<p data-block-id="a">vloženo</p><p data-block-id="a">znovu</p>');

    const result = ids();
    expect(result).toContain("a");
    expect(new Set(result).size).toBe(result.length);
    expect(result.filter((id) => id === "a")).toHaveLength(1);
  });

  it("prázdný dokument dostane blok s identifikátorem hned po otevření", () => {
    open([]);

    expect(ids()).toEqual(["new-1"]);
  });
});

describe("dokument schématu v1 (FR-7 akc. 1, 4, 6)", () => {
  it("všechny prvky FR-7 projdou načtením a uložením beze změny obsahu", () => {
    const blocks = (fixture as { content: JsonNode[] }).content;
    open(blocks);

    const saved = getBlocks(editor);
    setBlocks(editor, saved, generateId);
    const reopened = getBlocks(editor);

    expect(reopened).toEqual(saved);
    expect(saved.map((block) => block.type)).toEqual(blocks.map((block) => block.type));
    expect(saved.map((block) => block.attrs?.["id"])).toEqual(blocks.map((block) => block.attrs?.["id"]));
    expect(JSON.stringify(saved)).toContain('"href":"https://example.org"');
    expect(JSON.stringify(saved)).toContain('"checked":true');
  });

  it("neznámý typ bloku se zachová přesně (CNT-005)", () => {
    const unknown = (fixture as { content: JsonNode[] }).content.find((block) => block.type === "futureWidget")!;
    open([paragraph("a", "A"), unknown]);

    editor.chain().setTextSelection(startOfBlock(0) + 1).insertContent("x").run();

    expect(getBlocks(editor)[1]).toEqual(unknown);
  });

  it("blok s neznámou značkou i blok konfliktu se zachovají přesně", () => {
    const unknownMark: JsonNode = { type: "paragraph", attrs: { id: "m" }, content: [{ type: "text", text: "barva", marks: [{ type: "textColor", attrs: { color: "red" } }] }] };
    const conflict: JsonNode = { type: "conflict", attrs: { id: "c" }, content: [{ type: "conflictVariant", attrs: { side: "current" }, content: [paragraph("c", "x")] }] };
    open([unknownMark, conflict]);

    expect(getBlocks(editor)).toEqual([unknownMark, conflict]);
  });
});

describe("checklist a odkazy (FR-7 akc. 3, 7)", () => {
  it("položku checklistu jde označit a odznačit", () => {
    open([{ type: "taskList", attrs: { id: "t" }, content: [{ type: "taskItem", attrs: { checked: false }, content: [paragraph(null, "úkol")] }] }]);

    const checkbox = editor.view.dom.querySelector("input[type=checkbox]") as HTMLInputElement;
    checkbox.click();
    checkbox.dispatchEvent(new Event("change", { bubbles: true }));

    expect(JSON.stringify(getBlocks(editor))).toContain('"checked":true');
  });

  it.each([
    ["https://example.org", true],
    ["http://example.org", true],
    ["mailto:a@example.org", true],
    ["javascript:alert(1)", false],
    ["data:text/html,x", false],
    ["ftp://example.org", false],
    ["nesmysl", false],
  ])("odkaz %s povolen: %s", (href, allowed) => {
    expect(isAllowedLink(href)).toBe(allowed);
  });

  it("nepovolený odkaz se do dokumentu nedostane", () => {
    open([paragraph("a", "klik")]);

    editor.chain().setTextSelection({ from: startOfBlock(0), to: startOfBlock(0) + 4 }).setLink({ href: "javascript:alert(1)" }).run();
    expect(JSON.stringify(getBlocks(editor))).not.toContain("javascript:");

    setBlocks(editor, [{ type: "paragraph", attrs: { id: "b" }, content: [{ type: "text", text: "x", marks: [{ type: "link", attrs: { href: "javascript:alert(1)" } }] }] }], generateId);
    expect(JSON.stringify(getBlocks(editor))).not.toContain("javascript:");
    expect(texts()).toEqual(["x"]);
  });

  it("výběr textu v editoru funguje v testovacím DOM", () => {
    open([paragraph("a", "text")]);
    editor.view.dispatch(editor.state.tr.setSelection(TextSelection.create(editor.state.doc, 1, 3)));
    expect(editor.state.selection.empty).toBe(false);
  });
});

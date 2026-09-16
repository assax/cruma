import { Editor, Node, mergeAttributes } from "@tiptap/core";
import StarterKit from "@tiptap/starter-kit";
import Highlight from "@tiptap/extension-highlight";
import { TaskItem, TaskList } from "@tiptap/extension-list";
import { CrumaBlockIdExtension, assignBlockIds } from "./blockIds";
import { PreservedType, fromEditorContent, toEditorContent, type JsonNode } from "./schema";

/** Odkaz smí mít jen schéma http, https nebo mailto (FR-7 akc. 7, security-policy.md §3). */
export function isAllowedLink(href: string): boolean {
  try {
    return ["http:", "https:", "mailto:"].includes(new URL(href).protocol);
  } catch {
    return false;
  }
}

/** Blok, kterému editor nerozumí (novější typ, konflikt) – zobrazí se jako neměnný zástupce (CNT-005). */
const CrumaPreserved = Node.create({
  name: PreservedType,
  group: "block",
  atom: true,
  selectable: true,
  draggable: false,
  addAttributes() {
    return { kind: { default: "unknown" }, raw: { default: "{}" } };
  },
  parseHTML() {
    return [{ tag: "div[data-cruma-preserved]" }];
  },
  renderHTML({ HTMLAttributes, node }) {
    const text = node.attrs["kind"] === "conflict" ? "Konflikt – vyřešte ho v panelu konfliktů." : "Obsah z novější verze aplikace.";
    return ["div", mergeAttributes(HTMLAttributes, { "data-cruma-preserved": node.attrs["kind"], contenteditable: "false" }), text];
  },
});

export interface CrumaEditorOptions {
  element?: HTMLElement;
  blocks: JsonNode[];
  editable?: boolean;
  generateId?: () => string;
  onChange?: (blocks: JsonNode[]) => void;
}

/** Editor se schématem Cruma v1: jen prvky z FR-7 akc. 1, nic navíc (CNT-001). */
export function createCrumaEditor(options: CrumaEditorOptions): Editor {
  const generateId = options.generateId ?? (() => crypto.randomUUID());
  const editor = new Editor({
    element: options.element,
    editable: options.editable ?? true,
    extensions: [
      StarterKit.configure({
        blockquote: false,
        code: false,
        codeBlock: false,
        horizontalRule: false,
        trailingNode: false,
        heading: { levels: [1, 2, 3] },
        link: {
          openOnClick: false,
          autolink: true,
          protocols: ["http", "https", "mailto"],
          isAllowedUri: (url) => isAllowedLink(url),
          shouldAutoLink: (url) => isAllowedLink(url),
        },
      }),
      Highlight,
      TaskList,
      TaskItem.configure({ nested: false }),
      CrumaPreserved,
      CrumaBlockIdExtension.configure({ generateId }),
    ],
    content: { type: "doc", content: toEditorContent(options.blocks) },
    onUpdate: ({ editor: updated }) => options.onChange?.(getBlocks(updated)),
  });
  ensureIds(editor, generateId);
  return editor;
}

/** Bloky dokumentu z editoru. */
export function getBlocks(editor: Editor): JsonNode[] {
  return fromEditorContent((editor.getJSON().content ?? []) as JsonNode[]);
}

/** Nahradí obsah (např. po uložení nové verze jinde) bez vyvolání změny. */
export function setBlocks(editor: Editor, blocks: JsonNode[], generateId?: () => string): void {
  editor.commands.setContent({ type: "doc", content: toEditorContent(blocks) }, { emitUpdate: false });
  ensureIds(editor, generateId ?? (() => crypto.randomUUID()));
}

// Bloky bez identifikátoru při načtení (např. prázdný dokument) dostanou identifikátor hned.
function ensureIds(editor: Editor, generateId: () => string): void {
  const tr = editor.state.tr;
  assignBlockIds(tr, null, editor.state.doc, [], generateId);
  if (tr.docChanged) {
    tr.setMeta("addToHistory", false);
    editor.view.dispatch(tr);
  }
}

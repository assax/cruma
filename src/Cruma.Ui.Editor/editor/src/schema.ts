// Převod mezi dokumentem schématu Cruma.Content a obsahem editoru (CNT-005, CNT-006).
// Editor umí jen typy z registru schématu v1. Blok, který obsahuje cokoli jiného (typ z novější verze,
// neznámou značku, blok konfliktu), se v editoru drží jako neměnný uzel `crumaPreserved` s původním JSON
// a při uložení se vrátí přesně tak, jak přišel.

export type JsonNode = {
  type: string;
  attrs?: Record<string, unknown>;
  content?: JsonNode[];
  marks?: { type: string; attrs?: Record<string, unknown> }[];
  text?: string;
};

/** Bloky nejvyšší úrovně, které editor upravuje (FR-7 akc. 1, registr Cruma.Content v1). */
export const EditableBlockTypes = ["paragraph", "heading", "bulletList", "orderedList", "taskList"] as const;

const KnownNodeTypes = new Set<string>([...EditableBlockTypes, "listItem", "taskItem", "text", "hardBreak"]);
const KnownMarkTypes = new Set<string>(["bold", "italic", "underline", "strike", "link", "highlight"]);

export const PreservedType = "crumaPreserved";

function isEditable(node: JsonNode, topLevel: boolean): boolean {
  if (!KnownNodeTypes.has(node.type)) {
    return false;
  }
  if (topLevel && !(EditableBlockTypes as readonly string[]).includes(node.type)) {
    return false;
  }
  if (node.marks?.some((mark) => !KnownMarkTypes.has(mark.type))) {
    return false;
  }
  return (node.content ?? []).every((child) => isEditable(child, false));
}

/** Bloky dokumentu → obsah editoru. Prázdný dokument dostane jeden prázdný odstavec. */
export function toEditorContent(blocks: JsonNode[]): JsonNode[] {
  if (blocks.length === 0) {
    return [{ type: "paragraph" }];
  }
  return blocks.map((block) =>
    isEditable(block, true)
      ? withoutUnsafeLinks(block)
      : {
          type: PreservedType,
          attrs: { id: block.attrs?.["id"] ?? null, kind: block.type === "conflict" ? "conflict" : "unknown", raw: JSON.stringify(block) },
        },
  );
}

/** Odkaz s jiným schématem než http, https, mailto se nenačte – text zůstane, značka odkazu ne (FR-7 akc. 7). */
function withoutUnsafeLinks(node: JsonNode): JsonNode {
  const marks = node.marks?.filter((mark) => mark.type !== "link" || isSafeHref(mark.attrs?.["href"]));
  return {
    ...node,
    ...(node.marks ? { marks } : {}),
    ...(node.content ? { content: node.content.map(withoutUnsafeLinks) } : {}),
  };
}

function isSafeHref(href: unknown): boolean {
  try {
    return typeof href === "string" && ["http:", "https:", "mailto:"].includes(new URL(href).protocol);
  } catch {
    return false;
  }
}

/** Obsah editoru → bloky dokumentu. Zachované bloky se vrátí beze změny. */
export function fromEditorContent(content: JsonNode[]): JsonNode[] {
  return content.map((node) => (node.type === PreservedType ? (JSON.parse(String(node.attrs?.["raw"])) as JsonNode) : node));
}

import { Extension } from "@tiptap/core";
import { Plugin, PluginKey } from "@tiptap/pm/state";
import type { Node as ProseMirrorNode } from "@tiptap/pm/model";
import type { Transaction } from "@tiptap/pm/state";
import { Slice, Fragment } from "@tiptap/pm/model";
import { EditableBlockTypes, PreservedType } from "./schema";

export interface CrumaBlockIdOptions {
  /** Generátor nových identifikátorů bloků; v testech deterministický. */
  generateId: () => string;
}

const BlockTypes = [...EditableBlockTypes, PreservedType];

/**
 * Stabilní identifikátory bloků nejvyšší úrovně (CNT-002, content-document-pattern.md §1.2) – vlastní rozšíření
 * Cruma, ne placené rozšíření třetí strany (CNT-006):
 * - identifikátor přetrvá úpravu textu, přesun i změnu typu bloku,
 * - rozdělení ponechá identifikátor první části, druhá dostane nový,
 * - spojení ponechá identifikátor prvního bloku,
 * - vložený nebo duplikovaný blok dostane nový identifikátor.
 * Vnořené uzly (položky seznamů) identifikátor nenesou – patří bloku nejvyšší úrovně (§1.1).
 */
export const CrumaBlockIdExtension = Extension.create<CrumaBlockIdOptions>({
  name: "crumaBlockId",

  addOptions() {
    return { generateId: () => crypto.randomUUID() };
  },

  addGlobalAttributes() {
    return [
      {
        types: BlockTypes,
        attributes: {
          id: {
            default: null,
            keepOnSplit: false,
            parseHTML: (element) => element.getAttribute("data-block-id"),
            renderHTML: (attributes) => (attributes["id"] ? { "data-block-id": attributes["id"] } : {}),
          },
        },
      },
    ];
  },

  addProseMirrorPlugins() {
    const generateId = this.options.generateId;
    return [
      new Plugin({
        key: new PluginKey("crumaBlockId"),
        // Vložený obsah přichází bez identifikátorů – dostane nové (CNT-002).
        props: {
          transformPasted: (slice) => new Slice(stripIds(slice.content), slice.openStart, slice.openEnd),
        },
        appendTransaction: (transactions, oldState, newState) => {
          if (!transactions.some((transaction) => transaction.docChanged)) {
            return null;
          }
          const tr = newState.tr;
          assignBlockIds(tr, oldState.doc, newState.doc, transactions, generateId);
          return tr.docChanged ? tr : null;
        },
      }),
    ];
  },
});

/** Doplní identifikátory po změně dokumentu. Veřejné kvůli testům a počátečnímu načtení. */
export function assignBlockIds(
  tr: Transaction,
  oldDoc: ProseMirrorNode | null,
  newDoc: ProseMirrorNode,
  transactions: readonly Transaction[],
  generateId: () => string,
): void {
  const topLevel: { node: ProseMirrorNode; offset: number }[] = [];
  newDoc.forEach((node, offset) => topLevel.push({ node, offset }));

  const counts = new Map<string, number>();
  for (const { node } of topLevel) {
    const id = node.attrs["id"] as string | null;
    if (id) {
      counts.set(id, (counts.get(id) ?? 0) + 1);
    }
  }

  // Kam se po transakcích posunul každý blok starého dokumentu.
  const origins = new Map<string, number[]>();
  const claims = new Map<number, string>();
  oldDoc?.forEach((node, offset) => {
    const id = node.attrs["id"] as string | null;
    if (!id) {
      return;
    }
    const positions: number[] = [];
    for (const assoc of [-1, 1]) {
      let position = offset;
      let deleted = false;
      for (const transaction of transactions) {
        const result = transaction.mapping.mapResult(position, assoc);
        position = result.pos;
        deleted ||= result.deletedAfter && result.deletedBefore;
      }
      if (!deleted) {
        positions.push(position);
      }
    }
    origins.set(id, positions);
    if (positions.length > 0) {
      claims.set(positions[0], id);
    }
  });

  const used = new Set<string>();
  const decided = new Map<number, string>();

  // Duplicitní identifikátor si ponechá blok, který je posunutým původním blokem; ostatní kopie dostanou nový.
  for (const { node, offset } of topLevel) {
    const id = node.attrs["id"] as string | null;
    if (!id || !BlockTypes.includes(node.type.name)) {
      continue;
    }
    if ((counts.get(id) ?? 0) === 1) {
      decided.set(offset, id);
      used.add(id);
    }
  }
  for (const [id, count] of counts) {
    if (count < 2) {
      continue;
    }
    const candidates = topLevel.filter(({ node }) => node.attrs["id"] === id);
    const original = candidates.find(({ offset }) => origins.get(id)?.includes(offset)) ?? candidates[0];
    decided.set(original.offset, id);
    used.add(id);
  }

  for (const { node, offset } of topLevel) {
    if (!BlockTypes.includes(node.type.name)) {
      continue;
    }
    let id = decided.get(offset);
    if (!id) {
      // Blok bez identifikátoru na místě původního bloku (změna typu, zabalení do seznamu) převezme jeho identifikátor,
      // pokud ho nemá jiný blok.
      const claim = claims.get(offset);
      id = claim && !used.has(claim) ? claim : generateId();
      used.add(id);
    }
    if (node.attrs["id"] !== id) {
      tr.setNodeMarkup(tr.mapping.map(offset), undefined, { ...node.attrs, id });
    }
    node.descendants((child, childOffset) => {
      if (child.attrs["id"]) {
        tr.setNodeMarkup(tr.mapping.map(offset + 1 + childOffset), undefined, { ...child.attrs, id: null });
      }
    });
  }
}

function stripIds(fragment: Fragment): Fragment {
  const nodes: ProseMirrorNode[] = [];
  fragment.forEach((node) => {
    if (node.isText) {
      nodes.push(node);
      return;
    }
    const attrs = "id" in node.attrs ? { ...node.attrs, id: null } : node.attrs;
    nodes.push(node.type.create(attrs, stripIds(node.content), node.marks));
  });
  return Fragment.fromArray(nodes);
}

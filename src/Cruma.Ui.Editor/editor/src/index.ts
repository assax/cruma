// Jediný interop modul editoru (content-document-pattern.md §3): načtení dokumentu, hlášení změn
// s celým dokumentem, pojmenované příkazy. C# nikdy nečte stav TipTapu (CNT-006).
import type { Editor } from "@tiptap/core";
import { createCrumaEditor, getBlocks, isAllowedLink, setBlocks } from "./createEditor";
import type { JsonNode } from "./schema";

interface DotNetReference {
  invokeMethodAsync(method: string, ...args: unknown[]): Promise<unknown>;
}

interface Instance {
  editor: Editor;
  timer: ReturnType<typeof setTimeout> | undefined;
  flush: () => void;
}

const instances = new Map<number, Instance>();
let nextHandle = 1;

const ActiveFormats = ["bold", "italic", "underline", "strike", "highlight", "link", "bulletList", "orderedList", "taskList"];

/** Vytvoří editor v elementu; změny hlásí zpožděně metodou `OnContentChanged(json)`. */
export function create(element: HTMLElement, dotnet: DotNetReference, blocksJson: string, editable: boolean, debounceMs: number): number {
  const handle = nextHandle++;
  const instance: Instance = {
    editor: undefined as unknown as Editor,
    timer: undefined,
    flush: () => {
      if (instance.timer !== undefined) {
        clearTimeout(instance.timer);
        instance.timer = undefined;
        void dotnet.invokeMethodAsync("OnContentChanged", JSON.stringify(getBlocks(instance.editor)));
      }
    },
  };
  instance.editor = createCrumaEditor({
    element,
    editable,
    blocks: JSON.parse(blocksJson) as JsonNode[],
    onChange: () => {
      if (instance.timer !== undefined) {
        clearTimeout(instance.timer);
      }
      instance.timer = setTimeout(instance.flush, debounceMs);
    },
  });
  instance.editor.on("transaction", () => {
    const active = ActiveFormats.filter((name) => instance.editor.isActive(name));
    const heading = [1, 2, 3].find((level) => instance.editor.isActive("heading", { level }));
    void dotnet.invokeMethodAsync("OnFormatsChanged", heading ? [...active, `heading${heading}`] : active);
  });
  instances.set(handle, instance);
  return handle;
}

/** Nahradí obsah editoru (nová verze ze serveru) bez hlášení změny. */
export function setContent(handle: number, blocksJson: string): void {
  const instance = instances.get(handle);
  if (instance) {
    setBlocks(instance.editor, JSON.parse(blocksJson) as JsonNode[]);
  }
}

/** Okamžitě odešle čekající změnu (před uložením nebo opuštěním stránky). */
export function flush(handle: number): void {
  instances.get(handle)?.flush();
}

export function focus(handle: number): void {
  instances.get(handle)?.editor.commands.focus("end");
}

/** Pojmenovaný příkaz formátování. Vrací false, pokud příkaz nejde provést (např. nepovolený odkaz). */
export function execute(handle: number, command: string, argument?: string): boolean {
  const instance = instances.get(handle);
  if (!instance) {
    return false;
  }
  const chain = instance.editor.chain().focus();
  switch (command) {
    case "bold":
      return chain.toggleBold().run();
    case "italic":
      return chain.toggleItalic().run();
    case "underline":
      return chain.toggleUnderline().run();
    case "strike":
      return chain.toggleStrike().run();
    case "highlight":
      return chain.toggleHighlight().run();
    case "paragraph":
      return chain.setParagraph().run();
    case "heading1":
    case "heading2":
    case "heading3":
      return chain.toggleHeading({ level: Number(command.at(-1)) as 1 | 2 | 3 }).run();
    case "bulletList":
      return chain.toggleBulletList().run();
    case "orderedList":
      return chain.toggleOrderedList().run();
    case "taskList":
      return chain.toggleTaskList().run();
    case "link":
      return argument !== undefined && isAllowedLink(argument) ? chain.extendMarkRange("link").setLink({ href: argument }).run() : false;
    case "unlink":
      return chain.extendMarkRange("link").unsetLink().run();
    case "undo":
      return chain.undo().run();
    case "redo":
      return chain.redo().run();
    default:
      return false;
  }
}

export function destroy(handle: number): void {
  const instance = instances.get(handle);
  if (instance) {
    instance.flush();
    instance.editor.destroy();
    instances.delete(handle);
  }
}

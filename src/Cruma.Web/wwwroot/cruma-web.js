// Platformní služby prohlížeče pro Cruma.Web (ui-pattern.md §6). Jediné místo webového shellu s přístupem
// k API prohlížeče; sdílené UI je používá jen přes rozhraní v Cruma.Ui (UI-001).

// Připojení
export function isOnline() {
    return navigator.onLine;
}

export function watchConnectivity(dotnet) {
    const notify = () => dotnet.invokeMethodAsync("OnBrowserConnectivityChanged", navigator.onLine);
    window.addEventListener("online", notify);
    window.addEventListener("offline", notify);
}

// Předvolby UI (motiv, šířka) – jediná povolená lokální data vedle fronty (UI-004).
export function getPreference(key) {
    try {
        return localStorage.getItem(key);
    } catch {
        return null;
    }
}

export function setPreference(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch {
        // Úložiště nemusí být dostupné (soukromé okno); předvolba pak platí jen do zavření.
    }
}

// Fronta nových poznámek v IndexedDB (SYN-006): jen vytvoření, každá položka s identifikátorem od klienta.
const DatabaseName = "cruma";
const StoreName = "write-queue";

function openDatabase() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open(DatabaseName, 1);
        request.onupgradeneeded = () => request.result.createObjectStore(StoreName, { keyPath: "id" });
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
    });
}

async function withStore(mode, action) {
    const database = await openDatabase();
    try {
        return await new Promise((resolve, reject) => {
            const transaction = database.transaction(StoreName, mode);
            const result = action(transaction.objectStore(StoreName));
            transaction.oncomplete = () => resolve(result?.result);
            transaction.onerror = () => reject(transaction.error);
        });
    } finally {
        database.close();
    }
}

export function queuePut(item) {
    return withStore("readwrite", (store) => store.put(item));
}

export function queueRemove(id) {
    return withStore("readwrite", (store) => store.delete(id));
}

export async function queueList() {
    const items = await withStore("readonly", (store) => store.getAll());
    return (items ?? []).sort((a, b) => a.createdAtUtc.localeCompare(b.createdAtUtc));
}

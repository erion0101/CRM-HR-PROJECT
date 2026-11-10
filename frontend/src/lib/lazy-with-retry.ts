import { lazy, type ComponentType, type LazyExoticComponent } from 'react';

const reloadKey = 'hr-management:dynamic-import-reload';

export function isChunkLoadError(error: unknown): boolean {
  const message = error instanceof Error ? error.message : String(error);

  return /failed to fetch dynamically imported module|importing a module script failed|chunkloaderror|loading chunk .* failed/i.test(message);
}

function hasAlreadyReloaded(): boolean {
  try {
    return window.sessionStorage.getItem(reloadKey) === 'true';
  } catch {
    return false;
  }
}

function markReloaded(): void {
  try {
    window.sessionStorage.setItem(reloadKey, 'true');
  } catch {
    // Reloading still gives the browser a chance to obtain the latest entry point.
  }
}

function clearReloadMarker(): void {
  try {
    window.sessionStorage.removeItem(reloadKey);
  } catch {
    // Storage can be unavailable in privacy-restricted browser contexts.
  }
}

export function lazyWithRetry<T extends ComponentType>(loader: () => Promise<{ default: T }>): LazyExoticComponent<T> {
  return lazy(async () => {
    try {
      const loadedModule = await loader();
      clearReloadMarker();
      return loadedModule;
    } catch (error) {
      if (isChunkLoadError(error) && !hasAlreadyReloaded()) {
        markReloaded();
        window.location.reload();

        // Keep this render suspended while the browser replaces the old application.
        return new Promise<never>(() => undefined);
      }

      clearReloadMarker();
      throw error;
    }
  });
}

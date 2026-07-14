window.bookNThingsSettings = {
    pickStorageDirectory: async function (currentPath) {
        const result = window.prompt("Enter the storage folder path for this app.", currentPath ?? "");
        if (result === null) {
            return null;
        }

        const trimmed = result.trim();
        return trimmed.length > 0 ? trimmed : null;
    }
};

(() => {
    const retryIntervalMilliseconds = 5000;

    const startReconnectionProcess = () => {
        let isCanceled = false;

        (async () => {
            while(true) {
                await new Promise(resolve => setTimeout(resolve, retryIntervalMilliseconds));

                if (isCanceled) {
                    return;
                }

                try {
                    const result = await Blazor.reconnect();
                    if (!result) {
                        // The server was reached, but the connection was rejected; reload the page.
                        location.reload();
                        return;
                    }

                    // Successfully reconnected to the server.
                    return;
                } catch (exception) {
                }
            }
        })();

        return {
            cancel: () => {
                isCanceled = true;
            }
        };
    };

    let currentReconnectionProcess = null;

    Blazor.start({
        circuit: {
            reconnectionHandler: {
                onConnectionDown: () => currentReconnectionProcess ??= startReconnectionProcess(),
                onConnectionUp: () => {
                    currentReconnectionProcess?.cancel();
                    currentReconnectionProcess = null;
                }
            }
        }
    });
})();
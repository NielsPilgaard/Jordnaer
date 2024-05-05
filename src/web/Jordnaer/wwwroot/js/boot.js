(() => {
    const maximumRetryCount = 150;
    const retryIntervalMilliseconds = 5000;

    const startReconnectionProcess = () => {
        let isCanceled = false;

        (async () => {
            for (let i = 0; i < maximumRetryCount; i++) {
                console.log(`Attempting to reconnect: Attempt ${i + 1} of ${maximumRetryCount}`);
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
                    console.log('Failed to reach the server, trying again.');
                }
            }

            // Retried too many times; reload the page.
            location.reload();
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
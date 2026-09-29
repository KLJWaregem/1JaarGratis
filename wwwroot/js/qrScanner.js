const scanners = new Map();

export async function start(elementId, dotNetRef) {
    const scanner = new Html5Qrcode(elementId);
    scanners.set(elementId, scanner);
    
    let lastCode = null, lastAt = 0;
    await scanner.start(
        { facingMode: "environment" },
        { fps: 10, qrbox: 250 },
        code => {
            const now = Date.now();
            if (code === lastCode && now - lastAt < 500) return;
            lastCode = code; lastAt = now;
            dotNetRef.invokeMethodAsync("OnScanned", code);
        });
}

export async function stop(elementId) {
    const scanner = scanners.get(elementId);
    if (!scanner) return;
    scanners.delete(elementId);
    if (scanner.isScanning) await scanner.stop();
    scanner.clear();
}

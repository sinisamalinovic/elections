// Share QR code image via Web Share API
window.shareQrCode = async (base64png, bmName) => {
    try {
        const blob = await (await fetch(`data:image/png;base64,${base64png}`)).blob();
        const file = new File([blob], 'qr-posmatrac.png', { type: 'image/png' });
        if (navigator.canShare && navigator.canShare({ files: [file] })) {
            await navigator.share({
                files: [file],
                title: 'Посматрач QR',
                text: `Извештај: ${bmName}`
            });
            return true;
        }
    } catch (e) {
        console.warn('Share failed:', e);
    }
    return false;
};

// Download QR as PNG fallback
window.downloadQrCode = (base64png, filename) => {
    const link = document.createElement('a');
    link.href = `data:image/png;base64,${base64png}`;
    link.download = filename || 'qr-posmatrac.png';
    link.click();
};

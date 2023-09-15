window.copyToClipboard = function (text) {
    navigator.clipboard.writeText(text).then(function () {
        console.log('Copied to clipboard:', text);
    }).catch(function (err) {
        console.error('Could not copy to clipboard:', err);
    });
}  

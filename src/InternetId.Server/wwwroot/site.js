$(() => {
    $("[data-qrcode]").each(function () {
        let element = this;
        new QRCode(element, {
            text: element.getAttribute("data-qrcode"),
            width: 150,
            height: 150
        });
    });
});
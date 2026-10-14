(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) return;
    $.validator.addMethod("notinpast", function (value, element) {
        if (!value) return true;
        var entered = new Date(value + "T00:00:00");
        var today = new Date(); today.setHours(0,0,0,0);
        return entered >= today;
    });
    $.validator.unobtrusive.adapters.addBool("notinpast");
})(jQuery);

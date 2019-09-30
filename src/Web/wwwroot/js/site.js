$(function () {
    $('body').prepend('<div class="message" style="display:none"><span>X</span><div class="txt"></div><div class="controls"></div></div>');
    $.hideMessage = function () {
        clearTimeout($.messageTimer);
        $('.message').fadeOut(400);
    };

    $.showMessage = function (type, text, buttons) {
        var message = $('.message');
        message.removeClass('success');
        message.removeClass('error');
        message.addClass(type);
        message.find('.txt').text(text);
        message.find('span').click($.hideMessage);
        var controls = message.find('.controls');
        controls.empty();
        $.showMessageActions = [];
        for (var i = 0; i < buttons.length; i++) {
            var btn = buttons[i];
            var elem = $('<a href="javascript:;">' + btn.text + '</a>');
            elem.attr('no', i);
            $.showMessageActions.push(btn.action);
            elem.click(function () { $.hideMessage(); $.showMessageActions[$(this).attr('no')](); });

            controls.append(elem);
        }
        clearTimeout($.messageTimer);
        $.messageTimer = setTimeout($.hideMessage, 5000);
        message.fadeIn(400);
    };

    //$('h2').click(function() {
    //    $.showMessage('success',
    //        'Test test test test test Test test test test test Test test test test test  test test test Test test test test test',
    //        [
    //            {
    //                text: 'Ok',
    //                action: function() { alert('asdasd'); }
    //            }
    //        ])
    //});

    $.unlockJson = function(fn) {
        if (!getCookie('username')) {
            return true;
        }
        deleteCookie('username');
        if (typeof (fn) == 'function') {
            $.post($.unlockJsonUrl, fn);
        } else {
            $.post($.unlockJsonUrl);
        }
       
        return true;
    };

    $.forceUnlockJson = function (fn) {
        if (getCookie('username')) {
            deleteCookie('username');
        }
       
        if (typeof (fn) == 'function') {
            $.post($.unlockJsonUrl, fn);
        } else {
            $.post($.unlockJsonUrl);
        }

        return true;
    };
});
function getCookie(cName) {
    if (document.cookie.length > 0) {
        var cStart = document.cookie.indexOf(cName + '=');
        if (cStart !== -1) {
            cStart = cStart + cName.length + 1;
            var cEnd = document.cookie.indexOf(';', cStart);
            if (cEnd === -1) {
                cEnd = document.cookie.length;
            }
            return decodeURIComponent(document.cookie.substring(cStart, cEnd));
        }
    }

    return '';
}

function getAntiForgeryCookie() {
    return getCookie('XSRF-TOKEN');
}

var Request = (function () {
    function Request() { }

    Request.reloadAction = {
        reloadPage: false,
        div: '',
        url: '',
        data: {},
        showLoading: false
    };

    Request.doRequest = function(o) {
        if (!o.url) {
            throw 'url is empty';
        }

        o.params = o.formId ? $(o.formId).serialize() : o.params;

        this.setUpdateInfo(o);

        if (o.onStart)
            o.onStart();

        this.blockOnRequest();
        this.peformRequest(o);
    };

    Request.setUpdateInfo = function (r) {
        this.reloadAction.reloadPage = false;
        this.reloadAction.div = r.divResult;
        this.reloadAction.url = r.requestUrl;
        this.reloadAction.data = r.requestParams;
        this.reloadAction.showLoading = r.showLoading;
    };

    Request.peformRequest = function (o) {
        var _this = this;

        if (o.onShowLoading)
            o.onShowLoading();
        else {
            if (o.showLoading !== false && o.divResult) {
                $(o.divResult).html('<div style="text-align:center; margin-top:20px;"><img src="/images/loading.gif"></div>');
                $(o.divResult).show();
            }
        }

        var options = { url: o.url, type: 'POST', data: o.params, headers: { 'RequestVerificationToken': getAntiForgeryCookie() } };

        $.ajax(options)
            .then(function (result) {
                if (o.onHideLoading)
                    o.onHideLoading();
                if (o.onFinish)
                    o.onFinish();
                _this.unBlockOnRequest();
                _this.requestOkHandler(o, result);
            })
            .fail(function (jqXhr) {
                if (o.onHideLoading)
                    o.onHideLoading();
                if (o.onFinish)
                    o.onFinish();
                _this.unBlockOnRequest();
                _this.requestFailHandle(o, jqXhr.responseText, jqXhr);
            });
    };

    Request.requestOkHandler = function (o, result) {
        var _this = this;
        if (result.status === 'ErrorValidation') {
            $('.error').removeClass('error');
            if (result.field)
                $(result.field).addClass('error');
            return;
        }
        if (result.status === 'ErrorMessage') {
            if (result.field)
                $(result.field).text(result.msg);
            return;
        }
        if (result.status === 'Fail') {
            if (result.divError)
                o.divError = result.divError;
            o.placement = result.placement;
            this.requestFailHandle(o, result.msg);
            return;
        }
        if (result.status === 'Reload') {
            if (this.reloadAction.reloadPage)
                location.reload();
            else
                this.doRequest({ url: this.reloadAction.url, params: this.reloadAction.data, divResult: this.reloadAction.div, showLoading: this.reloadAction.showLoading });
            return;
        }
        if (result.refreshUrl) {
            this.doRequest({ url: result.refreshUrl, params: result.prms, divResult: result.div, showLoading: result.showLoading });
            return;
        }
        if (result.inputId) {
            $(result.inputId).val(result.text);
            return;
        }

        if (o.divResult) {
            if (o.replaceDiv) {
                $(o.divResult).replaceWith(result);
            }
            else {
                $(o.divResult).html(result);
            }
        }
    };

    Request.requestFailHandle = function (o, text, jqXhr) {
        if (jqXhr && jqXhr.status === 403) {
            if (o.divResult) {
                $(o.divResult).html('<div class="alert alert-danger" style="text-align:center; margin-top:20px;"><h1>Access denied</h1>You don\'t have an access to visit the ' + o.url + ' page.</div>');
            }
        }
        else {
            $.showMessage('error', text,[
                {
                    text: 'Ok',
                    action: function() {}
                }
            ]);
        }
    };

    Request.blockOnRequest = function () {
        $('.disableOnRequest').each(function () {
            $(this).attr('disabled', 'true');
        });
        $('.hideOnRequest').each(function () {
            $(this).css('display', 'none');
        });
        $('.showOnRequest').each(function () {
            $(this).css('display', 'inline');
        });
    };
    Request.unBlockOnRequest = function () {
        $('.disableOnRequest').each(function () {
            $(this).removeAttr('disabled');
        });
        $('.hideOnRequest').each(function () {
            $(this).css('display', 'inline');
        });
        $('.showOnRequest').each(function () {
            $(this).css('display', 'none');
        });
    };

    return Request;
}());

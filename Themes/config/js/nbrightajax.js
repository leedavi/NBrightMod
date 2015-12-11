


function NBrightMod_nbxget(cmd, selformdiv, target, selformitemdiv, appendreturn)
{
    $('.processing').show();

    $.ajaxSetup({ cache: false });

    var cmdupdate = '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?cmd=' + cmd;
    var values = '';
    if (selformitemdiv == null) {
        values = $.fn.genxmlajax(selformdiv);
    }
    else {
        values = $.fn.genxmlajaxitems(selformdiv, selformitemdiv);
    }
    var request = $.ajax({ type: "POST",
		url: cmdupdate,
		cache: false,
		data: { inputxml: encodeURI(values) }		
	});

    request.done(function(data) {
	    if (data != 'noaction') {
	        if (appendreturn == null) {
	            $(target).children().remove();
	            $(target).html(data).trigger('change');
	        } else
	            $(target).append(data).trigger('change');

	        $.event.trigger({
	            type: "NBrightMod_nbxgetcompleted",
	            cmd: cmd
	        });
	    }
	    if ((cmd != 'addselectedfiles') && (cmd != 'replaceselectedfiles') && (cmd != 'deleterecord') && (cmd != 'savedataexit') && (cmd != 'savedatareturn')) {
            $('.processing').hide();
        }
    });

	request.fail(function (jqXHR, textStatus) {
		alert("Request failed: " + textStatus);
		$('.processing').hide();
	});
}



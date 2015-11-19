
$(document).ready(function () {

    // set the default edit language to the current langauge
    $('#editlang').val($('#selectparams #lang').val());

    // get list of records via ajax:  NBrightMod_nbxget({command}, {div of data passed to server}, {return html to this div} )
    // If we only have a single page edit then the selectitemid param will be set, so display the detail.
    if ($('#selecteditemid').val() == "") {
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
    } else {
        NBrightMod_nbxget('getdetail', '#selectparams', '#editdata');
    }
    // Attach file upload button events
    ActivateFileUploadButtons();

});

$(document).on("NBrightMod_nbxgetcompleted", NBrightMod_nbxgetCompleted); // assign a completed event for the ajax calls

function NBrightMod_nbxgetCompleted(e) {

    if (e.cmd == 'selectlang') {
        // reload data, needed for after langauge switch
        if ($('#displayreturn').val() == 'list') {
            NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
        } else {
            NBrightMod_nbxget('getdetail', '#selectparams', '#editdata'); // do ajax call to get edit form
        }
    }

    if (e.cmd == 'savelistdata') {
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata'); // do ajax call to get edit form
    }

    if (e.cmd == 'savedataexit') {
        $('#selecteditemid').val('');
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata'); // do ajax call to get edit form
    }

    if (e.cmd == 'getdetail') {
        // Action after getdetail command
        $('#displayreturn').val('detail');

        $('.selecteditlanguage').click(function () {
            savedata();
            $('#editlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
            NBrightMod_nbxget('selectlang', '#editdata'); // do ajax call to save current edit form
        });

        $('#savedata').click(function () {
            savedata();
        });

        $('#savedataexit').click(function () {
            savedataexit();
        });

        $('#return').click(function() {
            $('#selecteditemid').val('');
            NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
        });

        $('#exitedit').click(function () {
            window.location.href = $('#exiturl').val();
        });

        $('#delete').click(function() {
            NBrightMod_nbxget('deleterecord', '#editdata');
        });

        $('.sortelementUp').click(function () { moveUp($(this).parent().parent().parent()); });
        $('.sortelementDown').click(function () { moveDown($(this).parent().parent().parent()); });
        $('.removeelement').click(function () { removeelement($(this).parent().parent().parent()); });
        $('#undoimage').click(function () { undoremove('.imageitem', '#imagelist'); });
        $('#undodoc').click(function () { undoremove('.docitem', '#doclist'); });

        ActivateFileLoader();

    }


    ActivateFileReturn(e);

    if (e.cmd == 'deleterecord') {
        $('#selecteddeleteid').val(''); // clear, it now doesn;t exists.
        $('#selecteditemid').val(''); // clear, it now doesn;t exists.
        // relist after delete
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
    }

    if (e.cmd == 'getlist' || e.cmd == 'addnew') {
        $('#displayreturn').val('list');

        if (e.cmd == 'addnew') $('#newitem').val(''); // clear item so if new was just created we don;t create another record

        // assign event on data return, otherwise the elemet will not be there, so it can't bind the event
        $('.edititem').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
            $('#displayreturn').val('detail');  // make sure we display the detail
            NBrightMod_nbxget('getdetail', '#selectparams', '#editdata'); // do ajax call to get edit form
        });

        $('.sortelementUp').click(function () { moveUp($(this).parent().parent().parent()); });
        $('.sortelementDown').click(function () { moveDown($(this).parent().parent().parent()); });

        $('#exitedit').click(function () {
            window.location.href = $('#exiturl').val();
        });

        $('#savelistdata').click(function () {
            NBrightMod_nbxget('savelistdata', '#editdatalist > tbody', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        });

        $('.selecteditlistlanguage').click(function () {
            $('#editlang').val($(this).attr('lang'));
            NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        });


        $('#addnew').click(function () {
            $('.processing').show();
            $('#newitem').val('new');
            $('#selecteditemid').val('');
            NBrightMod_nbxget('addnew', '#selectparams', '#editdata');
        });

        $('.deleteitemclick').click(function () {
            $('#selecteddeleteid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being deleted
            NBrightMod_nbxget('deleterecord', '#editdata');
        });

        ActivateFileLoader();

    }

}

function savedata() {
    var xmlrtn = $.fn.genxmlajaxitems('#imagelist > tbody', '.imageitem').replace(/<\!\[CDATA\[/g, "**CDATASTART**").replace(/\]\]>/g, "**CDATAEND**");
    var xmlrtn2 = $.fn.genxmlajaxitems('#doclist > tbody', '.docitem').replace(/<\!\[CDATA\[/g, "**CDATASTART**").replace(/\]\]>/g, "**CDATAEND**");
    $('#xmlupdateimages').val(xmlrtn);
    $('#xmlupdatedocs').val(xmlrtn2);
    NBrightMod_nbxget('savedata', '#editdata');
}

function savedataexit() {
    var xmlrtn = $.fn.genxmlajaxitems('#imagelist > tbody', '.imageitem').replace(/<\!\[CDATA\[/g, "**CDATASTART**").replace(/\]\]>/g, "**CDATAEND**");
    var xmlrtn2 = $.fn.genxmlajaxitems('#doclist > tbody', '.docitem').replace(/<\!\[CDATA\[/g, "**CDATASTART**").replace(/\]\]>/g, "**CDATAEND**");
    $('#xmlupdateimages').val(xmlrtn);
    $('#xmlupdatedocs').val(xmlrtn2);
    NBrightMod_nbxget('savedataexit', '#editdata');
}

function moveUp(item) {
    var prev = item.prev();
    if (typeof $(item).attr("fixedsort") !== typeof undefined && $(item).attr("fixedsort") !== false) return;
    if (typeof $(prev).attr("fixedsort") !== typeof undefined && $(prev).attr("fixedsort") !== false) return;
    if (prev.length == 0) return;
    prev.css('z-index', 999).css('position', 'relative').animate({ top: item.height() }, 1);
    item.css('z-index', 1000).css('position', 'relative').animate({ top: '-' + prev.height() }, 1, function () {
        prev.css('z-index', '').css('top', '').css('position', '');
        item.css('z-index', '').css('top', '').css('position', '');
        item.insertBefore(prev);
    });
}
function moveDown(item) {
    var next = item.next();
    if (typeof $(item).attr("fixedsort") !== typeof undefined && $(item).attr("fixedsort") !== false) return;
    if (typeof $(next).attr("fixedsort") !== typeof undefined && $(next).attr("fixedsort") !== false) return;
    if (next.length == 0) return;
    next.css('z-index', 999).css('position', 'relative').animate({ top: '-' + item.height() }, 1);
    item.css('z-index', 1000).css('position', 'relative').animate({ top: next.height() }, 1, function () {
        next.css('z-index', '').css('top', '').css('position', '');
        item.css('z-index', '').css('top', '').css('position', '');
        item.insertAfter(next);
    });
}
function removeelement(elementtoberemoved) {
    if ($('#recyclebin').length > 0) {
        $('#recyclebin').append($(elementtoberemoved));
    } else { $(elementtoberemoved).remove(); }
    if ($(elementtoberemoved).hasClass('imageitem')) $('#undoimage').show();
    if ($(elementtoberemoved).hasClass('docitem')) $('#undodoc').show();
}
function undoremove(itemselector, destinationselector) {
    if ($('#recyclebin').length > 0) {
        $(destinationselector).append($('#recyclebin').find(itemselector).last());
    }
    if ($('#recyclebin').children(itemselector).length == 0) {
        if (itemselector == '.imageitem') $('#undoimage').hide();
        if (itemselector == '.docitem') $('#undodoc').hide();
    }
}


function ActivateFileLoader() {


    $('.filelistclick').click(function () {
        $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
        NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        $('#displayreturn').val("list");
        $('#uploadtype').val("doc");
        $('.fileinput').show();
        $('#fileselectlist').children().remove();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
        $("#NBrightModModal").appendTo("body");
    });

    $('.fileclick').click(function () {
        savedata();
        $('#displayreturn').val("detail");
        $('#uploadtype').val("doc");
        $('.fileinput').show();
        $('#fileselectlist').children().remove();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
        $("#NBrightModModal").appendTo("body");
    });

    $('.imagelistclick').click(function () {
        $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
        NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        $('#displayreturn').val("list");
        $('#uploadtype').val("image");
        $('.fileinput').show();
        $('#fileselectlist').children().remove();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
        $("#NBrightModModal").appendTo("body");
    });

    $('.imageclick').click(function () {
        savedata();
        $('#displayreturn').val("detail");
        $('#uploadtype').val("image");
        $('.fileinput').show();
        $('#fileselectlist').children().remove();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
        $("#NBrightModModal").appendTo("body");
    });

    $('#fileselectlist').change(function () {
        // may need to do something here to stop display of image and docs when switching.
    });


}

function ActivateFileReturn(e) {

    if (e.cmd == 'getfolderfiles') {
        $('.fileselectitem').click(function () {
            //if we have a allow1selection class on the file list then one 1 can be selected, so hide all.
            if ($('.allow1selection')[0]) {
                $('.allow1selection').hide();
                $('#selectedfiles').val('');
            }

            var find = '.fileselector' + $(this).attr("selectorcount");
            if ($(find).is(':visible')) {
                $(find).hide();
                $('#selectedfiles').val($('#selectedfiles').val().replace($(this).attr("fname") + ',', ''));
            } else {
                $(find).show();
                var newf = $(this).attr("fname") + ',';
                $('#selectedfiles').val($('#selectedfiles').val() + newf);
            }
        });
    }

    if (e.cmd == 'addselectedfiles' || e.cmd == 'replaceselectedfiles') {
        CancelAddFiles();
        // commented to stop the processing div from hiding to early (not sure why we need this call, so I commented it out)
        //NBrightMod_nbxget('getfiles', '#selectparams', '#filelist');
    }
    if (e.cmd == 'deleteselectedfiles') {
        $('#selectedfiles').val('');
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
    }

    if (e.cmd == 'getfiles') {
        $(this).children().find('.sortelementUp').click(function () { moveUp($(this).parent()); });
        $(this).children().find('.sortelementDown').click(function () { moveDown($(this).parent()); });
        $('.removefile').click(function () { removeelement($(this).parent().parent()); });
        $('#undofile').click(function () { undoremove('.fileitem', '#filelistul'); });
    }

}

function ActivateFileUploadButtons() {
    $('.fileupload-button').click(function () {
        $('#fileupload').trigger('click');
    });

    $('#fileupload').change(function () {
        $('.processing').show();
        $('#fileselectlist').append('<div id="loader" class="processing"><i class="glyphicon glyphicon-cog"></i></div>');
        var fileSelect = document.getElementById('fileupload');

        if (!window.File || !window.FileReader || !window.FileList || !window.Blob) {
            alert('The File APIs are not fully supported in this browser.');
            return;
        }

        // Get the selected files from the input.
        var files = fileSelect.files;

        // Create a new FormData object.
        var formData = new FormData();

        // Loop through each of the selected files.
        for (var i = 0; i < files.length; i++) {
            var file = files[i];
            // Add the file to the request.
            formData.append('files[]', file, file.name);
        }

        // Set up the request.
        var xhr = new XMLHttpRequest();
        // Open the connection.
        xhr.open('POST', '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?mid=' + $('#moduleid').val() + '&cmd=fileupload&itemid=' + $('#selecteditemid').val(), true);

        // Set up a handler for when the request finishes.
        xhr.onload = function () {
            if (xhr.status != 200) {
                alert('An error occurred!');
            } else {
                $('#selectedfiles').val('');
                NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
            }
        };

        // Send the Data.
        xhr.send(formData);

    });

    $('#addselectedfiles').click(function () {
        NBrightMod_nbxget('addselectedfiles', '#selectparams', '#filelist');
    });

    $('#replaceselectedfiles').click(function () {
        NBrightMod_nbxget('replaceselectedfiles', '#selectparams', '#filelist');
    });

    $('#deleteselectedfiles').click(function () {
        $('#fileselectlist').append('<div id="loader" class="processing"><i class="glyphicon glyphicon-cog"></i></div>');
        NBrightMod_nbxget('deleteselectedfiles', '#selectparams', '#filelist');
    });

}

function CancelAddFiles() {
    $('#fileselectclose').trigger('click');
    $('#selectedfiles').val('');
    if ($('#displayreturn').val() == 'list') {
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
    } else {
        NBrightMod_nbxget('getdetail', '#selectparams', '#editdata'); // do ajax call to get edit form
    }
}
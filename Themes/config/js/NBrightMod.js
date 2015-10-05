
$(document).ready(function () {

    // set the default edit language to the current langauge
    $('#editlang').val($('#selectparams #lang').val());

    // get list of records via ajax:  NBrightMod_nbxget({command}, {div of data passed to server}, {return html to this div} )
    NBrightMod_nbxget('getlist', '#selectparams', '#editdata');

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
        // reload data, needed for after langauge switch
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata'); // do ajax call to get edit form
    }

    if (e.cmd == 'getdetail') {
        // Action after getdetail command
        $('#displayreturn').val('detail');

        $('.selecteditlanguage').click(function () {
            $('#editlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
            NBrightMod_nbxget('selectlang', '#editdata'); // do ajax call to save current edit form
        });

        $('#savedata').click(function () {
            NBrightMod_nbxget('savedata', '#editdata');
        });


        $('#return').click(function() {
            $('#selecteditemid').val('');
            NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
        });

        $('#delete').click(function() {
            NBrightMod_nbxget('deleterecord', '#editdata');
        });

        ActivateFileLoader();

    }

    ActivateFileReturn(e);

    if (e.cmd == 'deleterecord') {
        $('#selecteditemid').val(''); // clear sleecteditemid, it now doesn;t exists.
        // relist after delete
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
    }

    if (e.cmd == 'getlist' || e.cmd == 'addnew') {
        $('#displayreturn').val('list');

        if (e.cmd == 'addnew') $('#newitem').val(''); // clear item so if new was just created we don;t create another record

        // assign event on data return, otherwise the elemet will not be there, so it can't bind the event
        $('.edititem').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr("itemid")); // assign the sleected itemid, so the server knows what item is being edited
            $('#displayreturn').val('detail');  // make sure we display the detail
            NBrightMod_nbxget('getdetail', '#selectparams', '#editdata'); // do ajax call to get edit form
        });
        $('.itemup').click(function () {
            moveUp($(this).parent().parent().parent().parent());
        });
        $('.itemdown').click(function () {
            moveDown($(this).parent().parent().parent().parent());
        });

        $('#exitedit').click(function () {
            window.location.href = $('#exiturl').val();
        });

        $('#savelistdata').click(function () {
            NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
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
            $('#selecteditemid').val($(this).attr("itemid")); // assign the sleected itemid, so the server knows what item is being edited
            NBrightMod_nbxget('deleterecord', '#editdata');
        });

        ActivateFileLoader();

    }

}

function moveUp(item) {
    var prev = item.prev();
    if (prev.length == 0)
        return;
    prev.css('z-index', 999).css('position', 'relative').animate({ top: item.height() }, 1);
    item.css('z-index', 1000).css('position', 'relative').animate({ top: '-' + prev.height() }, 1, function () {
        prev.css('z-index', '').css('top', '').css('position', '');
        item.css('z-index', '').css('top', '').css('position', '');
        item.insertBefore(prev);
    });
}
function moveDown(item) {
    var next = item.next();
    if (next.length == 0)
        return;
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
}
function undoremove(itemselector, destinationselector) {
    if ($('#recyclebin').length > 0) {
        $(destinationselector).append($('#recyclebin').find(itemselector).last());
    }
    if ($('#recyclebin').children(itemselector).length == 0) {
        if (itemselector == '.imageitem') $('#undoimage').hide();
    }
}

function ShowFileOperation() {
    $("#editdata").hide();
    $("#fileoperation").show();
}

function HideFileOperation() {
    $("#editdata").show();
    $("#fileoperation").hide();
}


function ActivateFileLoader() {


    $('.filelistclick').click(function () {
        $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
        NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        $('#displayreturn').val("list");
        $('#uploadtype').val("doc");
        $('.fileinput').show();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
    });

    $('.fileclick').click(function () {
        NBrightMod_nbxget('savedata', '#editdata');
        $('#displayreturn').val("detail");
        $('#uploadtype').val("doc");
        $('.fileinput').show();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
    });

    $('.imagelistclick').click(function () {
        $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
        NBrightMod_nbxget('savelistdata', '#editdatalist', '#rtnmsg', '.datalistitem'); // do ajax post of list data.
        $('#displayreturn').val("list");
        $('#uploadtype').val("image");
        $('.fileinput').show();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
    });

    $('.imageclick').click(function () {
        NBrightMod_nbxget('savedata', '#editdata');
        $('#displayreturn').val("detail");
        $('#uploadtype').val("image");
        $('.fileinput').show();
        NBrightMod_nbxget('getfolderfiles', '#selectparams', '#fileselectlist');
    });

    $('#fileselectlist').change(function () {
        ShowFileOperation();
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
        $('#canceladdfiles').trigger('click');
        NBrightMod_nbxget('getfiles', '#selectparams', '#filelist');
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
                $('#canceladdfiles').trigger('click');
            }
        };

        // Send the Data.
        xhr.send(formData);

    });

    $('#canceladdfiles').click(function () {
        $('#selectedfiles').val('');
        HideFileOperation();
        if ($('#displayreturn').val() == 'list') {
            NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
        } else {
            NBrightMod_nbxget('getdetail', '#selectparams', '#editdata'); // do ajax call to get edit form
        }
    });

    $('#addselectedfiles').click(function () {
        NBrightMod_nbxget('addselectedfiles', '#selectparams', '#filelist');
        HideFileOperation();
    });

    $('#replaceselectedfiles').click(function () {
        NBrightMod_nbxget('replaceselectedfiles', '#selectparams', '#filelist');
        HideFileOperation();
    });

    $('#deleteselectedfiles').click(function () {
        NBrightMod_nbxget('deleteselectedfiles', '#selectparams', '#filelist');
    });

}

$(document).ready(function () {

    // set the default edit language to the current langauge
    $('#editlang').val($('#selectparams #lang').val());

    // get list of records via ajax:  NBrightMod_nbxget({command}, {div of data passed to server}, {return html to this div} )
    NBrightMod_nbxget('getsettings', '#selectparams', '#editdata');

});

$(document).on("NBrightMod_nbxgetcompleted", NBrightMod_nbxgetCompleted); // assign a completed event for the ajax calls

function NBrightMod_nbxgetCompleted(e) {

    if (e.cmd == 'selectlang') {
        // reload data, needed for after langauge switch
        NBrightMod_nbxget('getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
    }

    if (e.cmd == 'getdata') {
        // Action after getdata command

        $('.imageclick').click(function () {
            $('.imginput').show();
            $('#addimages').show();
            ShowDialog(true);
        });


        $('.selecteditlanguage').click(function () {
            $('#editlang').val($(this).attr('lang')); // alter lang after, so we get correct data record
            NBrightMod_nbxget('selectlang', '#editdata'); // do ajax call to save current edit form
        });

        $('.editdata #savedata').click(function () {
            NBrightMod_nbxget('savedata', '#editdata');
        });

        $('.editdata #return').click(function() {
            $('#selecteditemid').val('');
            NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
        });

        $('.editdata #delete').click(function() {
            NBrightMod_nbxget('deleterecord', '#editdata');
        });

        $('input[type="radio"][name="rbllinkradio"]').click(function () {
            displaylinkfields($(this).val());
        });

        displaylinkfields($('input[type="radio"][name="rbllinkradio"]:checked').val());

        // IMAGES - START
        $('.imageupload-button').click(function () {
            $('#imageupload').trigger('click');
        });

        $('#imageupload').change(function () {
            $('.processing').show();
            var fileSelect = document.getElementById('imageupload');

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
                // Check the file type.
                if (!file.type.match('image.*')) {
                    continue;
                }
                // Add the file to the request.
                formData.append('images[]', file, file.name);
            }

            // Set up the request.
            var xhr = new XMLHttpRequest();
            // Open the connection.
            xhr.open('POST', '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?mid=' + $('#moduleid').val() + '&cmd=fileupload&itemid=' + $('#itemid').val(), true);

            // Set up a handler for when the request finishes.
            xhr.onload = function () {
                if (xhr.status != 200) {
                    alert('An error occurred!');
                } else {
                    NBrightMod_nbxget('getfolderimages', '#selectparams', '#imageselectlist');
                    $('#canceladdimages').trigger('click');
                }
            };

            // Send the Data.
            xhr.send(formData);

        });

        $('#canceladdimages').click(function () {
            $('#imageselectlist').hide();
            $('#addselectedimages').hide();
            $('#deleteselectedimages').hide();
            $('#uploadimages').hide();
            $(this).hide();
            $('#selectedimages').val('');
            HideDialog();
            NBrightMod_nbxget('getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
        });

        $('#addselectedimages').click(function () {
            NBrightMod_nbxget('addselectedimages', '#selectparams', '#imageselectlist');
            HideDialog();
        });
        $('#deleteselectedimages').click(function () {
            NBrightMod_nbxget('getfolderimages', '#selectparams', '#imageselectlist');
        });

        // get the list of images to display
        NBrightMod_nbxget('getfolderimages', '#selectparams', '#imageselectlist');

        // IMAGES - END


    }

    //IMG ----------------------------------------------------------
    if (e.cmd == 'getfolderimages') {
        $('.imageselectitem').click(function () {
            if ($('.' + $(this).attr("name")).is(':visible')) {
                $('.' + $(this).attr("name")).hide();
                $('#selectedimages').val($('#selectedimages').val().replace($(this).attr("fname") + ',', ''));
            } else {
                $('.' + $(this).attr("name")).show();
                var newf = $(this).attr("fname") + ',';
                $('#selectedimages').val($('#selectedimages').val() + newf);
            }
        });

    }

    if (e.cmd == 'addselectedimages') {
        $('#canceladdimages').trigger('click');
        NBrightMod_nbxget('getimages', '#selectparams', '#imagelist');
    }
    if (e.cmd == 'deleteselectedimages') {
        $('#selectedimages').val('');
        NBrightMod_nbxget('getfolderimages', '#selectparams', '#imageselectlist');
    }

    //IMG ----------------------------------------------------------


    if (e.cmd == 'addnew') {
        $('#newitem').val(''); // clear item so if new was just created we don;t create another record

        // assign event on data return, otherwise the elemet will not be there, so it can't bind the event
        $('.edititem').click(function () {
            $('#selecteditemid').val($(this).attr("itemid")); // assign the selected itemid, so the server knows what item is being edited
            NBrightMod_nbxget('getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
        });
    }

    if (e.cmd == 'deleterecord') {
        $('#selecteditemid').val(''); // clear sleecteditemid, it now doesn;t exists.
        // relist after delete
        NBrightMod_nbxget('getlist', '#selectparams', '#editdata');
    }

    if (e.cmd == 'getlist') {

        // assign event on data return, otherwise the elemet will not be there, so it can't bind the event
        $('.edititem').click(function () {
            $('.processing').show();
            $('#selecteditemid').val($(this).attr("itemid")); // assign the sleected itemid, so the server knows what item is being edited
            NBrightMod_nbxget('getdata', '#selectparams', '#editdata'); // do ajax call to get edit form
        });

        $('#exitedit').click(function () {
            window.location.href = $('#exiturl').val();
        });

        $('#addnew').click(function () {
            $('.processing').show();
            $('#newitem').val('new');
            $('#selecteditemid').val('');
            NBrightMod_nbxget('addnew', '#selectparams', '#editdata');
        });
    }

}

function displaylinkfields(value) {
    if (value == "0") {
        $('.externallink').hide();
        $('.internallink').hide();
        $('.linktext').hide();
    }
    if (value == "1") {
        $('.externallink').show();
        $('.internallink').hide();
        $('.linktext').show();
    }
    if (value == "2") {
        $('.externallink').hide();
        $('.internallink').show();
        $('.linktext').show();
    }

}


function moveUp(item) {
    var prev = item.prev();
    if (prev.length == 0)
        return;
    prev.css('z-index', 999).css('position', 'relative').animate({ top: item.height() }, 250);
    item.css('z-index', 1000).css('position', 'relative').animate({ top: '-' + prev.height() }, 300, function () {
        prev.css('z-index', '').css('top', '').css('position', '');
        item.css('z-index', '').css('top', '').css('position', '');
        item.insertBefore(prev);
    });
}
function moveDown(item) {
    var next = item.next();
    if (next.length == 0)
        return;
    next.css('z-index', 999).css('position', 'relative').animate({ top: '-' + item.height() }, 250);
    item.css('z-index', 1000).css('position', 'relative').animate({ top: next.height() }, 300, function () {
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

function ShowDialog(modal) {
    $("#overlay").show();
    $("#dialog").fadeIn(300);

    if (modal) {
        $("#overlay").unbind("click");
    }
    else {
        $("#overlay").click(function (e) {
            HideDialog();
        });
    }
}

function HideDialog() {
    $("#overlay").hide();
    $("#dialog").fadeOut(300);
}





$(document).ready(function () {

    // get list of records via ajax:  NBrightMod_nbxget({command}, {div of data passed to server}, {return html to this div} )
    NBrightMod_nbxget('getsettings', '#selectparams', '#editdata');

});

$(document).on("NBrightMod_nbxgetcompleted", NBrightMod_nbxgetCompleted); // assign a completed event for the ajax calls

function NBrightMod_nbxgetCompleted(e) {

    if (e.cmd == 'savetheme') {
        NBrightMod_nbxget('getsettings', '#themedata', '#settingsdata');
    }
    if (e.cmd == 'gettheme') {
        // Action after getdata command

        $('#export').click(function () {
            NBrightMod_nbxget('exporttheme', '#nbrightmodsettings', '#returnfile');
        });

        $('#returnfile').change(function () {
            if ($(this).text().substr(0, 5) == "ERROR") {
                $(this).show();
            } else {
                $(this).hide();
                window.location.href = '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?cmd=downloadfile&filename=' + $(this).text();
            }
        });

        // ************ UPLOAD START ************
        $('#import').click(function () {
            $('#docupload').trigger('click');
        });

        $('#docupload').change(function () {
            $('.processing').show();
            var fileSelect = document.getElementById('docupload');
            if (!window.File || !window.FileReader || !window.FileList || !window.Blob) {
                alert('The File APIs are not fully supported in this browser.');
                return;
            }
            if (fileSelect.files.length >= 1) {
                var files = fileSelect.files;
                var formData = new FormData();
                formData.append('docs[]', files[0], files[0].name);
                var xhr = new XMLHttpRequest();
                xhr.open('POST', '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?mid=' + $('#moduleid').val() + '&cmd=importtheme', true);
                xhr.onload = function () {
                    if (xhr.status != 200) {
                        alert('An error occurred!');
                    } else {
                        location.reload();
                    }
                };
                xhr.send(formData);
            }
        });
        // ************ UPLOAD END ************

    }

}

function saveresxdata() {
    // The resxdata is in list format we therefore need to convert CDATA, we can't do that in the template becuase the template itself will covert.
    // There we use this function to place the required data format for passback in the correct field.
    var xmlrtn3 = $.fn.genxmlajaxitems('#resxtextboxes > p', '.itemresx').replace(/<\!\[CDATA\[/g, "**CDATASTART**").replace(/\]\]>/g, "**CDATAEND**");
    $('#xmlupdateresx').val(xmlrtn3);
}


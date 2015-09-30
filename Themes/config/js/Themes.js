
$(document).ready(function () {

    // get list of records via ajax:  NBrightMod_nbxget({command}, {div of data passed to server}, {return html to this div} )
    NBrightMod_nbxget('getsettings', '#selectparams', '#editdata');

});

$(document).on("NBrightMod_nbxgetcompleted", NBrightMod_nbxgetCompleted); // assign a completed event for the ajax calls

function NBrightMod_nbxgetCompleted(e) {

    if (e.cmd == 'getsettings') {
        // Action after getdata command

        $(function () { $("#tabs").tabs(); });

        $('.editdata #savedata').click(function () {
            NBrightMod_nbxget('savetheme', '#themedata');
        });

        $('.editdata #exitedit').click(function() {
            window.location.href = $('#exiturl').val();
        });

        $('#export').click(function () {
            NBrightMod_nbxget('exporttheme', '#selectparams', '#returnfile');
        });

        $('#returnfile').change(function () {
            window.location.href = '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?cmd=downloadfile&filename=' + $(this).text();
        });


    }
    if (e.cmd == 'savetheme') {
        window.location.href = $('#exiturl').val();
    }

}



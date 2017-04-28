

$(document).ready(function () {
    ActivateFileLoader();
});

function s4() {
    return Math.floor((1 + Math.random()) * 0x10000).toString(16).substring(1);
}

function ActivateFileLoader() {

        var uqid = s4();
        var filecount = 0;
        var filesdone = 0;
        var filestotal = 0;

        $('#optionfileprefix').val(uqid);
        $('input[id*="optionfilelist"]').val('');

        $(function () {
            'use strict';
            // Change this to the location of your server-side upload handler:
            var url = '/DesktopModules/NBright/NBrightMod/XmlConnector.ashx?cmd=clientfileupload&itemid=' + uqid;

            $('#optionfile').fileupload({
                url: url,
                maxFileSize: 5000000,
                acceptFileTypes: /(\.|\/)(pdf|zip|gif|jpe?g|png)$/i,
                sequentialUploads: true,
                dataType: 'json'
            }).prop('disabled', !$.support.fileInput).parent().addClass($.support.fileInput ? undefined : 'disabled')
                .bind('fileuploadprogressall', function (e, data) {
                    $('.fileuploadlist').hide();
                    $('.proceesingupload').show();
                })
                .bind('fileuploadadd', function (e, data) {
                    $.each(data.files, function (index, file) {
                        $('#optionfilelist').val($('#optionfilelist').val() + file.name + ',');
                        $('.fileuploadlist').append("<tr><td>" + file.name + "</td></tr>");
                        filesdone = filesdone + 1;
                    });
                }).bind('fileuploadchange', function (e, data) {
                    filecount = data.files.length;
                })
                .bind('fileuploaddrop', function (e, data) {
                    filecount = data.files.length;
                }).bind('fileuploadstop', function (e) {
                    if (filesdone == filecount) {
                        filestotal += filesdone;
                        filesdone = 0;
                        $('.fileuploadlist').show();
                        $('.proceesingupload').hide();
                    }
                    if (filestotal >= 5) {
                        $('.fileUpload').hide();
                    }
                });
        });

}
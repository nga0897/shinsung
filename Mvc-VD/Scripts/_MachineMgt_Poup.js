    $("#dialogDangerous").dialog({
        width: '20%',
        height: 100,
        maxWidth: '20%',
        maxHeight: 100,
        minWidth: '20%',
        minHeight: 100,
        resizable: false,
        fluid: true,
        modal: true,
        autoOpen: false,
        classes: {
            "ui-dialog": "ui-dialog",
            "ui-dialog-titlebar": "ui-dialog ui-dialog-titlebar-sm",
            "ui-dialog-titlebar-close": "visibility: hidden",
        },
        resize: function (event, ui) {
            $('.ui-dialog-content').addClass('m-0 p-0');
        },
        open: function (event, ui) {

        },
    });

    $("#deletestyle").click(function () {
        $.ajax({
            url: "/DevManagement/deleteMachineMgt",
            type: "post",
            dataType: "json",
            data: {
                m_mno: $("#m_mno").val(),
            },
            success: function (result) {
                if (result != 0) {
                    jQuery("#MachineMgtGrid").setGridParam({ datatype: "json" }).trigger('reloadGrid');
                }
                else {
                    alert('Machine Infor was not existing. Please check again');
                }
            },
            error: function (result) { }
        });
        $('#dialogDangerous').dialog('close');
    });

    $('#closestyle').click(function () {
        $('#dialogDangerous').dialog('close');
    });

    $('#del_save_but').click(function () {
        $('#dialogDangerous').dialog('open');
    });



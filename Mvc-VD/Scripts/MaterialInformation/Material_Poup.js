$(function () {
    $(".dialog2").dialog({
        width: '50%',
        height: 500,
        maxWidth: 1000,
        maxHeight: 500,
        minWidth: '50%',
        minHeight: 500,
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

            $("#popupMaterial").jqGrid
            ({
                url: "/Shippingmgt/Getpopupmaterial",
                datatype: 'json',
                mtype: 'Get',
                colModel: [
                   { label: 'ID', name: 'mtid', key: true, width: 50, align: 'center', hidden: true },
                   { label: 'Type', name: 'mt_type', sortable: true, width: 80, align: 'center' },
                   { label: 'Code', name: 'mt_no', key: true, width: 200, align: 'left' },
                   { label: 'Name', name: 'mt_nm', sortable: true, width: 200, align: 'left' },
                   { label: 'Width', name: 'width', sortable: true, width: 100, align: 'right' },
                   { label: 'Spec', name: 'spec', sortable: true, width: 100, align: 'right' },
                   { label: 'Photo', name: 'photo_file', width: 100, formatter: downloadLink, align: 'center' },
                   { label: 'Photo', name: 'photo_file', width: 100, hidden: true },
                   { label: 'Price', name: 'price', width: 150, align: 'right' },
                   { label: 'Re Mark', name: 'remark', width: 130, cellattr: function (rowId, cellValue, rowObject) { return ' title="' + cellValue + '"'; } },
                ],
                onSelectRow: function (rowid, selected, status, e) {
                    var selectedRowId = $("#popupMaterial").jqGrid("getGridParam", 'selrow');
                    row_id = $("#popupMaterial").getRowData(selectedRowId);
                    $("#selected").removeClass("disabled");
                    if (row_id != null) {
                        $("#selected").click(function () {
                        $('#c_mt_no').val(row_id.mt_no);
                        $('#m_mt_no').val(row_id.mt_no);
                        $('#s_mt_nm').val(row_id.mt_nm);
                        $('.dialog2').dialog('close');
                        });

                    }
                },

                pager: jQuery('#pagerMaterial'),
                viewrecords: true,
                rowList: [20, 50, 200, 500],
                height: 250,
                width: $(".boxM").width(),
                rowNum:50,
                autowidth: false,
                caption: 'Material Information',
                loadtext: "Loading...",
                emptyrecords: "No data.",
                rownumbers: true,
                gridview: true,
                shrinkToFit: false,
                multiselect: false,
                loadonce: true,
                jsonReader:
                {
                    root: "rows",
                    page: "page",
                    total: "total",
                    records: "records",
                    repeatitems: false,
                    Id: "0"
                },


            });

        },
    });
    function downloadLink(cellValue, options, rowdata, action) {
        if (cellValue != null) {
            var html = '<img src="../images/' + cellValue + '" style="height:20px;" />';
            return html;

        } else {
            return "";
        }
    }

    $(".poupdialogMaterial").click(function () {
        $('.dialog2').dialog('open');
    });

    $("#searchBtn_pp_mt").click(function () {
        var type = $("#mt_type").val().trim();
        var code = $("#mt_no").val().trim();
        var name = $("#mt_nm").val();
        $.ajax({
            url: "/Shippingmgt/searchmaterial",
            type: "get",
            dataType: "json",
            data: {
                typeData: type,
                codeData: code,
                nameData: name,
            },
            success: function (result) {
                $("#popupMaterial").jqGrid('clearGridData').jqGrid('setGridParam', { datatype: 'local', data: result }).trigger("reloadGrid");
            }
        });
    });

    $('#closestyle').click(function () {
        $('.dialog2').dialog('close');
    });

});
$(document).ready(function () {
    _GetType();
});
function _GetType() {
    $.get("/Shippingmgt/GetType", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            html += '<option value="" selected="selected">*Type*</option>';
            $.each(data, function (key, item) {
                html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
            });
            $("#mt_type").html(html);
        }
    });
}
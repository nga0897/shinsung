﻿$(function () {
    $(".diologbobin").dialog({
        width: '50%',
        height: 520,
        maxWidth: 1000,
        maxHeight: 450,
        minWidth: '50%',
        minHeight: 450,
        zIndex: 1000,
        resizable: false,
        fluid: true,
        modal: true,
        autoOpen: false,
        classes: {
            "ui-dialog": "ui-dialog",
            "ui-dialog-titlebar": "ui-dialog ui-dialog-titlebar-sm",
            "ui-button ui-widget ui-state-default ui-corner-all ui-button-icon-only ui-dialog-titlebar-close": "display: none !important",
        },
        resize: function (event, ui) {
            $('.ui-dialog-content').addClass('m-0 p-0');
        },
        open: function (event, ui) {
            $("#popupbobin").jqGrid
                ({
                    /* datatype: 'json',*/
                    //mtype: 'Get',
                    datatype: function (pdata) { getDataListBobbin(pdata); },
                    mtype: 'Get',
                    colModel: [
                        { key: true, label: 'bno', name: "bno", width: 80, align: 'center', hidden: true },
                        { key: false, label: 'Container Code', name: 'bb_no', width: 300, align: 'center', sort: true },
                        { key: false, label: 'Material', name: 'mt_cd', width: 400, align: 'left' },
                    ],
                    onSelectRow: function (rowid, selected, status, e) {

                        $("#save_bb").removeClass("disabled");
                        var selectedRowId = $("#popupbobin").jqGrid("getGridParam", 'selrow');
                        row_id = $("#popupbobin").getRowData(selectedRowId);
                        if (row_id != null) {
                            $('#close_bb').click(function () {
                                $('.diologbobin').dialog('close');
                            });
                            $('#mt_cd_bb').val(row_id.mt_cd);
                            $('#cp_bb_no2').val(row_id.bb_no);
                        }
                    },
                    pager: jQuery('#PageBobbin'),
                    viewrecords: true,
                    rowList: [50, 100, 200, 500, 1000],
                    height: 250,
                    width: null,
                    autowidth: true,
                    rowNum: 50,
                    caption: 'Container List',
                    loadtext: "Loading...",
                    emptyrecords: "No data.",
                    rownumbers: true,
                    gridview: true,
                    loadonce: false,
                    shrinkToFit: false,
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
    $('#close_bb').click(function () {
        $('.diologbobin').dialog('close');
        $("#s_bb_no").val("");
        $("#s_bb_nm").val("");
        $("#mt_code_bb").val("");
       /* jQuery("#popupbobin").setGridParam({ rowNum: 50, datatype: "json" }).trigger('reloadGrid');*/
    });
});
//$(".poupdialogbb").click(function () {
//  /*  jQuery("#popupbobin").setGridParam({ rowNum: 50, datatype: "json" }).trigger('reloadGrid');*/
//    $('.diologbobin').dialog('open');
//    get_ds_bb();
//});
function get_ds_bb() {
    //$.ajax({
    //    url: "/ActualWO/searchbobbinPopup",
    //    type: "get",
    //    dataType: "json",
    //    data: {
    //        bb_no: $('#s_bb_no').val().trim(),
    //        bb_nm: $('#s_bb_nm').val().trim(),
    //        mt_cd: $('#mt_code_bb').val().trim(),
    //        id_actual: $('#id_actual').val(),
    //    },
    //    success: function (result) {
    //        $("#popupbobin").jqGrid('clearGridData').jqGrid('setGridParam', { rowNum: 50, datatype: 'local', data: result }).trigger("reloadGrid");
    //    }
    //});
    $('#popupbobin').clearGridData();
    $('#popupbobin').jqGrid('setGridParam', { search: true });
    var pdata = $('#popupbobin').jqGrid('getGridParam', 'postData');
    getDataListBobbin(pdata);
}
$("#searchBtn_popupbobbin").click(function () {
    get_ds_bb();
});



function getDataListBobbin(pdata) {
    var params = new Object();

    if ($('#popupbobin').jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }

    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params._search = pdata._search;
   
    params.bb_no = $("#s_bb_no").val().trim();
    params.bb_nm = $("#s_bb_nm").val().trim();
    params.mt_cd = $("#mt_code_bb").val().trim();
    params.id_actual = $('#id_actual').val(),

        $('#popupbobin').jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });

    $.ajax({
        url: "/ActualWO/searchbobbinPopup",
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
            if (st == "success") {
                var showing = $('#popupbobin')[0];
                showing.addJSONData(data);
            }
        },
        error: function () {
            return;
        }
    });
};
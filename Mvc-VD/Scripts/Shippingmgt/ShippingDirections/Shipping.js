
$(function () {
    $("#list1").jqGrid
    ({
        url: "/Shippingmgt/getshipinfo",
        datatype: 'json',
        mtype: 'Get',
        colModel: [
             { key: true, label: 'sid', name: 'sid', width: 30, align: 'center', hidden: true },
             { label: 'SD No', name: 'sd_no', width: 200, align: 'center', formatter: titlebom },
             { label: 'SD No', name: 'sd_no', width: 120, align: 'center', hidden: true },
             { label: 'Destination', name: 'lct_cd', width: 150, align: 'left' },
             { label: 'Status', name: 'sd_sts_cd', width: 100, align: 'left' },
             { label: ' Work Date', name: 'work_dt', width: 150, align: 'center' },
             { label: 'Worker', name: 'worker_id', width: 110, align: 'left' },
             { label: 'Manager', name: 'manager_id', width: 150, align: 'left' },
             { label: 'MT Qty', name: 'mt_qty', width: 100, align: 'right', formatter: 'integer' },
        ],
        formatter: {
            integer: { thousandsSeparator: ",", defaultValue: '0' },
            currency: { decimalSeparator: ".", defaultValue: '0.00', prefix: "", suffix: "", thousandsSeparator: ',', decimalPlaces: 2 },
            number: { decimalSeparator: ".", thousandsSeparator: " ", decimalPlaces: 2, defaultValue: '0.00' },
        },
        onSelectRow: function (rowid, selected, status, e) {

            var selectedRowId = $("#list1").jqGrid("getGridParam", 'selrow');
            row_id = $("#list1").getRowData(selectedRowId);

            var sid = row_id.sid;
            var sd_no = row_id.sd_no;

            $('#m_sd_no').val(sd_no);
            $("#list2").clearGridData();
            $("#list2").setGridParam({ url: "/Shippingmgt/getReDetails?" + "sd_no=" + sd_no, datatype: "json" }).trigger("reloadGrid");
            $("#list3").setGridParam({ url: "/Shippingmgt/getship3?" + "sd_no=" + sd_no, datatype: "json" }).trigger("reloadGrid");
        },
        pager: $('#PagegridAu_Mem'),
        pager: "#PagegridAu_Mem",
        rowNum: 10,
        loadonce: true,
        viewrecords: true,
        rowList: [10, 20, 30, 40, 50, 100],
        height: '245',
        caption: '',
        loadtext: "Loading...",
        emptyrecords: "No data.",
        rownumbers: true,
        gridview: true,

        shrinkToFit: false,
        autowidth: true,
        multiselect: false,
        jsonReader:
            {
                root: "rows",
                page: "page",
                total: "total",
                records: "records",
                repeatitems: false,
                Id: "0"
            },

    })

});

function titlebom(cellvalue, options, rowobject) {
    var sid = rowobject.sid;
    if (cellvalue != null) {
        var html = '<a href="#" data-toggle="modal"   style="color: dodgerblue;" data-target="#modifyModal" data-backdrop="static" data-keyboard="false" onclick="Title(' + sid + ');" class="poupgbom">' + cellvalue + '</a>';
        return html;
    } else {
        return cellvalue;
    }
}
function Title(sid) {
    $("#pp_sid").val(sid);
    $('.picking').dialog('open');
    $.ajax({
        url: "/Shippingmgt/getpp_sd_no?id=" + sid,
        type: "post",
        dataType: "json",
        success: function (result) {
            var html = '<td class="text-center">' + result.sd_no + '</td>';
            html += '<td>' + result.lct_cd + '</td>';
            html += '<td  class="text-center">' + result.work_dt + '</td>';
            html += '<td>' + result.worker_id + '</td>';
            html += '<td>' + result.manager_id + '</td>';
            $("#popup_sd_no").html(html);
            var html1 = "PD-SD-" + result.sd_no;
            $('#barcode').append(html1);
            $('#barcode').barcode(html1, "code128", { barWidth: 1, barHeight: 30 });

            $("#title1").html("<h1 style='font-size: 43px;'>Shipping Direction</h1><h2 style='font-size: 20px;'>" + result.sd_no + " " + '[' + result.lct_cd + " " + ']' + "</h2>");
        },
    });
    $('#popupmate_sd').DataTable({
        "bServerSide": true,
        "ajax": {
            "url": '/Shippingmgt/getppReDetails?id=' + sid,
            "type": "Post",
            "datatype": "json"
        },
        "searching": false,
        "paging": false,
        "bInfo": false,
        "columns": [
            { "data": "mt_no" },
            { "data": "mt_nm" },
            { "data": "d_mt_qty" },
            { "data": "width" },
            { "data": "spec" },
        ],
        'columnDefs': [
        {
            "targets": 0, // your case first column
            "className": "text-left",
        },
        {
            "targets": 1, // your case first column
            "className": "text-left",
        },
        {
            "targets": 2, // your case first column
            "className": "text-right",
        },
        {
            "targets": 3, // your case first column
            "className": "text-right",
        },
        {
            "targets": 4, // your case first column
            "className": "text-right",
        },
        ],
        'rowsGroup': [0, 1],
        "bDestroy": true,

    });

    $('#popupmate_list').DataTable({
        "bServerSide": true,
        "ajax": {
            "url": '/Shippingmgt/getpp_mt_list?id=' + sid,
            "type": "Post",
            "datatype": "json"
        },
        "searching": false,
        "paging": false,
        "bInfo": false,
        "columns": [
            { "data": "mt_cd" },
            { "data": "gr_qty" },
            { "data": "lct_cd" },
            { "data": "mt_no" },
            { "data": "mt_nm" },
            { "data": "mt_sts_cd" },
            { "data": "lct_sts_cd" },
        ],
        'columnDefs': [
        {
            "targets": 0, // your case first column
            "className": "text-left",
        },
        {
            "targets": 1, // your case first column
            "className": "text-right",
        },
        {
            "targets": 2, // your case first column
            "className": "text-left",
        },
        {
            "targets": 3, // your case first column
            "className": "text-left",
        },
        {
            "targets": 4, // your case first column
            "className": "text-left",
        },
        {
            "targets": 5, // your case first column
            "className": "text-left",
        },
        {
            "targets": 6, // your case first column
            "className": "text-left",
        },
        ],
        'rowsGroup': [0, 1],
        "bDestroy": true,

    });
}

$("#PrintDetail").on("click", function () {
    var id = $("#pp_sid").val();
    window.open("/Shippingmgt/Print_picking_direction?" + "id=" + id, "PRINT", "width=1100, height=800, left=0, top=100, location=no, status=no,")
});//end click

$(function () {
    $("#list2").jqGrid
    ({
        url: "/Shippingmgt/getReDetails",
        datatype: 'json',
        mtype: 'Get',
        colModel: [
            { key: true, label: 'sdid', name: 'sdid', width: 50, hidden: true },
            { key: false, label: 'SD NO', name: 'sd_no', width: 110, align: 'center' },
            { label: 'MT NO  ', name: 'mt_no', width: 150, align: 'left' },
            { label: 'MT CD', name: 'mt_cd', width: 200, align: 'left' },
            { label: 'Group Qty', name: 'gr_qty_mt', sortable: true, width: 100, align: 'right', },
            { label: 'Unit', name: 'unit_cd', sortable: true, width: 60, align: 'left', hidden: true },
            { label: 'Send Bundle Qty', name: 'req_bundle_qty', sortable: true, width: 100, align: 'right' },
            { label: 'Send Qty', name: 'req_qty', sortable: true, width: 100, align: 'right' },
            { label: 'Width (mm)', name: 'new_with', sortable: true, width: 80, align: 'right' },
            { label: 'Length (M)', name: 'new_spec', sortable: true, width: 100, align: 'right' },
            { label: 'Area(m²)', name: 'area_all', width: 150, align: 'right' },
            { label: 'Ml Qty', name: 'count_detail', width: 110, align: 'right', formatter: 'integer' },
        ],
        formatter: {
            integer: { thousandsSeparator: ",", defaultValue: '0' },
            currency: { decimalSeparator: ".", defaultValue: '0.00', prefix: "", suffix: "", thousandsSeparator: ',', decimalPlaces: 2 },
            number: { decimalSeparator: ".", thousandsSeparator: " ", decimalPlaces: 2, defaultValue: '0.00' },
        },
        onSelectRow: function (rowid, status, e) {

            var selectedRowId = $("#list2").jqGrid("getGridParam", 'selrow');
            row_id = $("#list2").getRowData(selectedRowId);
            var sdid = row_id.sdid;
            var mt_no = row_id.mt_no;

            $('#m_sdid').val(sdid);


            $('#m_sd_no').val(sd_no);
            $('#m_mt_no').val(mt_no);
        },
        pager: jQuery('#PageGetJqAuMn'),
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        rownumbers: true,
        autowidth: true,
        shrinkToFit: false,
        viewrecords: true,
        loadonce: true,
        height: '220',
        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },


    })
});


$(function () {
    $("#list3").jqGrid
    ({
        url: "/Shippingmgt/getship3",
        datatype: 'json',
        mtype: 'Get',
        colModel: [
             { key: true, label: 'sdmid', name: 'sdmid', width: 50, hidden: true },
             { key: false, label: 'SD NO', name: 'sd_no', width: 110, align: 'center' },
             { label: 'ML NO', name: 'mt_cd', width: 300, align: 'left' },
             { label: 'Group Qty', name: 'gr_qty_unit', width: 110, align: 'right' },
             { label: 'From Location', name: 'lct_cd', width: 200, align: 'left' },
             { label: 'Destination', name: 'to_lct_cd', width: 200, align: 'left' },
             { label: 'MT No', name: 'mt_no', width: 200, align: 'left' },
             { label: 'MT Name', name: 'mt_nm', width: 200, align: 'left' },
             { label: 'Status', name: 'mt_sts_cd', width: 110, align: 'left' },
             { label: 'Location Status', name: 'lct_sts_cd', width: 110, align: 'left' },
             //{ label: 'QR Code', name: 'mt_barcode', width: 250, align: 'left' },
        ],
        pager: "#list3Pager",
        loadonce: true,
        userDataOnFooter: true,
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        rownumbers: true,
        viewrecords: true,
        emptyrecords: "No data.",
        gridview: true,
        caption: 'Material List',
        loadonce: true,
        rownumbers: true,
        autowidth: true,
        shrinkToFit: false,
        height: 300,
        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },


    })


});

//$('#list3').jqGrid('setGridWidth', $(".boxC").width());
//$(window).on("resize", function () {
//    var newWidth = $("#list3").closest(".ui-jqgrid").parent().width();
//    $("#list3").jqGrid("setGridWidth", newWidth, false);
//});

$("#searchBtn").click(function () {
    var sd_no = $('#sd_no').val().trim();
    var lct_cd = $('#lct_cd').val().trim();
    var start = $('#start').val().trim();
    var end = $('#end').val().trim();
    var worker_id = $('#worker_id').val().trim();
    var mt_no = $('#mt_no').val().trim();


    $.ajax({
        url: "/Shippingmgt/searchRequest",
        type: "get",
        dataType: "json",
        data: {

            sd_no: sd_no,
            lct_cd: lct_cd,
            start: start,
            end: end,
            worker_id: worker_id,
            mt_no: mt_no,

        },
        success: function (result) {
            $("#list1").jqGrid('clearGridData').jqGrid('setGridParam', { datatype: 'local', data: result }).trigger("reloadGrid");
        }
    });

});
$("#end").datepicker({ dateFormat: 'yy-mm-dd' }).val();
$("#start").datepicker({ dateFormat: 'yy-mm-dd' }).val();

//$('#end').datepicker().datepicker('setDate', 'today');

$(function () {
    $(".picking").dialog({
        width: '70%',
        height: 1100,
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
            "ui-dialog-titlebar-close": "visibility: hidden",
        },
        resize: function (event, ui) {
            $('.ui-dialog-content').addClass('m-0 p-0');
        },
    });


    $('#closesd_no').click(function () {
        $('#popupmate_sd').empty();
        $('#popupmate_list').empty();
        $('.picking').dialog('close');
    });

});

$('#excelBtn').click(function () {

    $('#exportData').attr('action', "/ShippingMgt/ExportToExcelDirections");

});

$('#htmlBtn').click(function (e) {
    $("#list1").jqGrid('exportToHtml',
        options = {
            title: '',
            onBeforeExport: null,
            includeLabels: true,
            includeGroupHeader: true,
            includeFooter: true,
            tableClass: 'jqgridprint',
            autoPrint: false,
            topText: '',
            bottomText: '',
            fileName: "Directions (S)",
            returnAsString: false
        }
        );
});
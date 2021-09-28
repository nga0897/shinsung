$(".dialog_worker").dialog({
    width: '50%',
    height: 500,
    maxWidth: 1000,
    maxHeight: 450,
    minWidth: '50%',
    minHeight: 450,
    resizable: false,
    fluid: true,
    modal: true,
    autoOpen: false,
    classes: {
        "ui-dialog": "ui-dialog",
        "ui-dialog-titlebar": "ui-dialog ui-dialog-titlebar-sm",
        //"ui-dialog-titlebar-close": "visibility: hidden",
    },
    resize: function (event, ui) {
        $('.ui-dialog-content').addClass('m-0 p-0');
    },
    open: function (event, ui) {

        $("#popupworker").jqGrid
        ({
            datatype: 'json',
            mtype: 'Get',
            colModel: [
                 { key: true, label: 'psid', name: 'psid', align: 'center', hidden: true },
                { key: false, label: 'Staff Id', name: 'staff_id', width: 100, align: 'left', },
                { key: false, label: 'Name', name: 'uname', sortable: true, width: '200', align: 'left', },
                { key: false, label: 'Start Date', name: 'start_dt', width: '200', formatter: "date", formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" } },
                { key: false, label: 'End Date', name: 'end_dt', width: '200', formatter: "date", formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" } },
                { key: false, label: 'Use', name: 'use_yn', editable: true, width: '50', align: 'center', width: 200, },
            ],
            formatter: {
                integer: { thousandsSeparator: ",", defaultValue: '0' },
                currency: { decimalSeparator: ".", defaultValue: '0.00', prefix: "", suffix: "", thousandsSeparator: ',', decimalPlaces: 2 },
                number: { decimalSeparator: ".", thousandsSeparator: " ", decimalPlaces: 2, defaultValue: '0.00' },
            },
            onSelectRow: function (rowid, selected, status, e) {
                $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
                var selectedRowId = $("#popupworker").jqGrid("getGridParam", 'selrow');
                row_id = $("#popupworker").getRowData(selectedRowId);

                $("#psid").val(row_id.psid);
                $("#m_staff_id").val(row_id.staff_id);
                $("#m3_use_yn").val(row_id.use_yn);
                $("#m2_start").val(row_id.start_dt);
                $("#m2_end").val(row_id.end_dt);

                $("#tab_5").removeClass("active");
                $("#tab_6").addClass("active");
                $("#tab_c5").removeClass("active");
                $("#tab_c6").removeClass("hidden");
                $("#tab_c5").addClass("hidden");
                $("#tab_c6").addClass("active");
                $("#m3_save_but").attr("disabled", false);
                $("#del_save_but_1").attr("disabled", false);
            },

            pager: '#PageMTworker',
            viewrecords: true,
            rowNum: 50,
            rowList: [50, 100, 200, 500, 1000],
            sortable: true,
            loadonce: true,
            height: 250,
            width: $(".boxStaff").width() - 5,
            loadtext: "Loading...",
            emptyrecords: "No data.",
            rownumbers: true,
            gridview: true,
            shrinkToFit: false,
            multiselect: true,
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

$(".Popup_wk_pr").on("click", function (){
    _get_staff();
    $('.dialog_worker').dialog('open');
    $.ajax({
        url: "/ActualWO/Getprocess_staff",
        type: "get",
        dataType: "json",
        data: {
            id_actual: ($('#id_actual').val() == "" ? 0 : $('#id_actual').val()),
        },
        success: function (response) {
           //debugger;
            $("#popupworker").jqGrid('clearGridData').jqGrid('setGridParam', { datatype: 'local', data: response }).trigger("reloadGrid");
            
        },

    });
});
$("#tab_5").on("click", "a", function (event) {
    $("#tab_6").removeClass("active");
    $("#tab_5").addClass("active");
    $("#tab_c6").removeClass("active");
    $("#tab_c5").removeClass("hidden");
    $("#tab_c6").addClass("hidden");
    $("#tab_c5").addClass("active");
    $('#start2').datetimepicker({
        format: 'YYYY-MM-DD HH:mm:ss',
        daysOfWeekDisabled: [0, 6],
    });
    $('#end2').datetimepicker({
        format: 'YYYY-MM-DD HH:mm:ss',
        date: new Date('9999-12-31 23:59:59'),
    });
});
$("#tab_6").on("click", "a", function (event) {
    $("#tab_5").removeClass("active");
    $("#tab_6").addClass("active");
    $("#tab_c5").removeClass("active");
    $("#tab_c6").removeClass("hidden");
    $("#tab_c5").addClass("hidden");
    $("#tab_c6").addClass("active");
    $("#m3_save_but").attr("disabled", true);
    $('#m2_start').datetimepicker({
        format: 'YYYY-MM-DD HH:mm:ss',
        daysOfWeekDisabled: [0, 6],
    });
    $('#m2_end').datetimepicker({
        format: 'YYYY-MM-DD HH:mm:ss',
        date: new Date('9999-12-31 23:59:59'),
    });
});
function _get_staff() {
  
    $.get("/ActualWO/get_staff", function (data) {
        if (data != null && data != undefined && data.length) {
            //console.log(data);
            var html = '';
            html += '<option value="" disabled selected>*Staff type*</option>';
            $.each(data, function (key, item) {
                html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
            });

            $("#c_staff").html(html);
            $("#m_staff").html(html);
        }
    });
}
$('#m2_start').datetimepicker({
    format: 'YYYY-MM-DD HH:mm:ss',
});
$('#m2_end').datetimepicker({
    format: 'YYYY-MM-DD HH:mm:ss',
});

$("#c3_save_but").click(function () {

    if ($("#form5").valid() == true) {
        $.ajax({
            url: "/ActualWO/Createprocess_unitstaff",
            type: "get",
            dataType: "json",
            data: {
                staff_id: $('#c_staff_id').val(),
                id_actual: $('#id_actual').val(),
                use_yn: $('#c2_use_yn').val(),
            },
            success: function (data) {
                if (data.result) {
                    $("#popupworker").jqGrid('addRowData', data.kq.psid, data.kq, 'first');
                        $("#popupworker").setRowData(data.kq.psid, false, { background: "#d0e9c6" });
                        $("#so_luong_worker").val(1);
                }
                else {
                    ErrorAlert(data.message);
                }
                //switch (data.result) {

                //    case 0:
                //        $("#popupworker").jqGrid('addRowData', data.kq.psid, data.kq, 'first');
                //        $("#popupworker").setRowData(data.kq.psid, false, { background: "#d0e9c6" });
                //        $("#so_luong_worker").val(1);
                //        break;
                //    case 1:
                //        alert('The Staff Unit was setting duplicate time.');
                //        break;
                //    case 2:
                //        alert('Start day was bigger End day. That is wrong');
                //        break;
                //    case 3:
                //        call_create("create_wk", data.update, data.start, data.end);
                //}
               
            }
        });
    }
});
$("#m3_save_but").click(function () {
    if ($("#form6").valid() == true) {
        $.ajax({
            type: "get",
            dataType: "json",
            url: "/ActualWO/Modifyprocess_unitstaff",
            data: {
                psid: $('#psid').val(),
                staff_id: $('#m_staff_id').val(),
                use_yn: $('#c2_use_yn').val(),
                start: $('#m2_start').val(),
                end: $('#m2_end').val(),

            },
            success: function (data) {
           
                switch (data.result) {
                    case 0:
                        var id=$('#psid').val();
                        $("#popupworker").setRowData(id, data.kq, { background: "#d0e9c6" });
                        $("#popupworker").setRowData(id, false, { background: "#d0e9c6" });
                        break;
                    case 1:
                        alert('The Staff Unit was setting duplicate time.');
                        break;
                    case 2:
                        alert('Start day was bigger End day. That is wrong');
                        break;
                    case 4:
                        alert(data.message);
                        break;
                }
            }
        });
    }
});
$("#form5").validate({
    rules: {
        "staff_id": {
            required: true,

        },
        "staff": {
            required: true,

        },
        "start2": {
            required: true,

        },
        "end2": {
            required: true,

        },
    },
});

$("#form6").validate({
    rules: {
        "staff_id": {
            required: true,

        },
        "staff": {
            required: true,

        },
        "start2": {
            required: true,

        },
        "end2": {
            required: true,

        },
    },
});


$(".dialogManu").dialog({
    width: '50%',
    height: 450,
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
        "ui-dialog-titlebar-close": "visibility: hidden",
    },
    resize: function (event, ui) {
        $('.ui-dialog-content').addClass('m-0 p-0');
    },
    open: function (event, ui) {

        $("#popupmanu").jqGrid
        ({
            url: '/DevManagement/GetPopupManu',
            datatype: 'json',
            mtype: 'Get',
            colModel: [
               { key: true, label: 'mfno', name: "mfno", width: 80, align: 'center', hidden: true },
               { key: false, label: 'Code', name: 'mf_cd', width: 110, align: 'left' },
               { key: false, label: 'Name', name: 'mf_nm', width: 150, align: 'left' },
               { key: false, label: 'Brd Name', name: 'brd_nm', width: 200, align: 'left' },
               { label: 'Photo', name: 'logo', width: 100, formatter: downloadlogo, align: 'center' },
               { key: false, label: 'Phone number', name: 'phone_nb', width: 300, align: 'left' },
               { key: false, label: 'Web Site', name: 'web_site', width: 90, align: 'center' },
               { key: false, label: 'Address', name: 'address', align: 'center', width: 150 },
               { key: false, label: 'Description', name: 're_mark', width: 90, align: 'center' },
            ],
            onSelectRow: function (rowid, selected, status, e) {
                $("#savestyle_Manu").removeClass("disabled");
                $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
                var selectedRowId = $("#popupmanu").jqGrid("getGridParam", 'selrow');
                row_id = $("#popupmanu").getRowData(selectedRowId);
                if (row_id != null) {
                    $("#savestyle_Manu").click(function () {
                        $('#c_manufac').val(row_id.mf_cd);
                        $('#m_manufac').val(row_id.mf_cd);
                        $('#pp_manufac').val(row_id.mf_cd);
                        $('.dialogManu').dialog('close');
                    });
                }
            },

            pager: '#pagermanu',
            viewrecords: true,
            rowNum: 50,
            rowList: [50, 100, 200, 500, 1000],
            loadonce: true,
            height: 220,
            width: $(".dialogManu").width() - 30,
            autowidth: false,
            loadtext: "Loading...",
            emptyrecords: "No data.",
            rownumbers: true,
            gridview: true,
            shrinkToFit: false,
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


        });

    },
});
function downloadlogo(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '<img src="../images/' + cellValue + '" style="height:20px;" />';
        return html;

    } else {
        return "";
    }
}

$(".poupdialogmanu").click(function () {
    $('.dialogManu').dialog('open');
});

$('#closestyle_Manu').click(function () {
    $('.dialogManu').dialog('close');
});

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

$(".dialogSupli").dialog({
    width: '50%',
    height: 450,
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
        "ui-dialog-titlebar-close": "visibility: hidden",
    },
    resize: function (event, ui) {
        $('.ui-dialog-content').addClass('m-0 p-0');
    },
    open: function (event, ui) {

        $("#popupsupllier").jqGrid
        ({
            url: '/DevManagement/getpopupsuplier',
            datatype: 'json',
            mtype: 'Get',
            colModel: [
                    { label: 'spno', name: 'spno', key: true, width: 80, align: 'center', hidden: true },
                    { label: 'Supplier Code', name: 'sp_cd', width: 150, align: 'center' },
                    {
                        label: 'Supplier Name', name: 'sp_nm', width: 150, sorttype: false, sortable: false,
                        cellattr: function (rowId, cellValue, rowObject) {
                            return ' title="' + cellValue + '"';
                        }
                    },
                { label: 'Busines type', name: 'bsn_tp', width: 80, align: 'center' },
                { label: 'BSN', name: 'bsn_tp', width: 80, hidden: true, align: 'center' },
                { label: 'Changer', name: 'chg_nm', width: 80, align: 'center', },
                { label: 'Tel', name: 'phone_nb', width: 90, align: 'left' },
                { label: 'Cell', name: 'cell_nb', width: 90, align: 'left' },
                { label: 'Fax', name: 'fax_nb', width: 90, align: 'left' },
                { label: 'E-Mail', name: 'e_mail', width: 120, align: 'left', sorttype: false, sortable: false, cellattr: function (rowId, cellValue, rowObject) { return ' title="' + cellValue + '"'; } },
                { label: 'Website', name: 'web_site', width: 120, align: 'left' },
                { label: 'Address', name: 'address', width: 150, align: 'left', sorttype: false, sortable: false, cellattr: function (rowId, cellValue, rowObject) { return ' title="' + cellValue + '"'; } },
                { label: 'Remark', name: 're_mark', width: 120, align: 'left' },
            ],
            onSelectRow: function (rowid, selected, status, e) {
                $("#savestyle_Supli").removeClass("disabled");
                $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
                var selectedRowId = $("#popupsupllier").jqGrid("getGridParam", 'selrow');
                row_id = $("#popupsupllier").getRowData(selectedRowId);
                if (row_id != null) {
                    $("#savestyle_Supli").click(function () {
                        $('#c_supllier').val(row_id.sp_cd);
                        $('#m_supllier').val(row_id.sp_cd);
                        $('#pp_supllier').val(row_id.sp_cd);
                        $('.dialogSupli').dialog('close');
                    });
                }
            },

            pager: '#pagersupllier',
            loadonce: true,
            viewrecords: true,
            rowNum: 50,
            rowList: [50, 100, 200, 500, 1000],
            height: 220,
            width: $(".dialogSupli").width()-30,
            autowidth: false,
            loadtext: "Loading...",
            emptyrecords: "No data.",
            rownumbers: true,
            gridview: true,
            shrinkToFit: false,
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


        });

    },
});
function downloadlogo(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '<img src="../Images/ManufactImg' + cellValue + '" style="height:20px;" />';
        return html;

    } else {
        return "";
    }
}

$(".poupdialogsupli").click(function () {
    $('.dialogSupli').dialog('open');
});

$('#closestyle_Supli').click(function () {
    $('.dialogSupli').dialog('close');
});

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// Popup Dangerous
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

$('#del_save_but').click(function () {
    $('#dialogDangerous').dialog('open');
});


$("#deletestyle").click(function () {
    var id = $('#m_mtid').val();
    $.ajax({
        url: "/DevManagement/deleteMaterial",
        type: "post",
        dataType: "json",
        data: {
            id: id,
        },
        success: function (resposive) {
            if (resposive.result) {
                $("#list").jqGrid('delRowData', id);
                SuccessAlert(resposive.message);
            }
            else {
                ErrorAlert(resposive.message);
            }
        },
        error: function (result) { }
    });
    $('#dialogDangerous').dialog('close');
});

$('#closestyle').click(function () {
    $('#dialogDangerous').dialog('close');
});

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

$(".dialogImg").dialog({
    width: '60%',
    height: 700,
    maxWidth: 1000,
    maxHeight: 450,
    minWidth: '40%',
    minHeight: 450,
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

$('#closeImg').click(function () {
    $('.dialogImg').dialog('close');
});

//copy_function
$(".Copy_material").dialog({
    width: '90%',
    height: 400,
    maxWidth: 1000,
    maxHeight: 450,
    minWidth: '40%',
    minHeight: 450,
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
    //close: function (event, ui) {

    //    document.getElementById("form3").reset();
    //},
});

$("#m_copy").click(function () {

    //$('.copy_consum option[value=val2]').attr('selected', 'selected');
    $('.Copy_material').dialog('open');
    $("#pp_mt_type").val("PMT");
});


$("#pp_save_but").click(function () {
    if ($("#form3").valid() == true) {

        var formData = new FormData();
        var files = $("#pp_photo_file").get(0).files;
        formData.append("file", files[0]);
        formData.append("mt_type", $('#pp_mt_type').val());
        formData.append("mt_no", $('#pp_mt_no').val().toUpperCase());
        formData.append("mt_nm", $('#pp_mt_nm').val());
        formData.append("gr_qty", $('#pp_gr_qty').val());
        formData.append("re_mark", $('#pp_re_mark').val());
        formData.append("sp_cd", $('#pp_supllier').val());
        formData.append("mf_cd", $('#pp_manufac').val());
        formData.append("s_lot_no", $('#pp_s_lot_no').val());

        if ($('#pp_width').val() == "") { formData.append("width", "0") } else { formData.append("width", $('#pp_width').val()); }
        if ($('#pp_spec').val() == "") { formData.append("spec", "0") } else { formData.append("spec", $('#pp_spec').val()); }
        if ($('#pp_price').val() == "") { formData.append("price", "0") } else { formData.append("price", $('#pp_price').val()); }
        if ($('#pp_area').val() == "") { formData.append("area", "0") } else { formData.append("area", $('#pp_area').val()); }
        formData.append("width_unit", $('#pp_width_unit').val());
        formData.append("spec_unit", $('#pp_spec_unit').val());
        formData.append("price_unit", $('#pp_price_unit').val());
        formData.append("area_unit", $('#pp_area_unit').val());
        formData.append("qc_range_cd", $('#pp_qc_range').val());
        formData.append("item_vcd", $('#pp_item_vcd').val());
        formData.append("unit_cd", $('#pp_unit_cd').val());
        formData.append("mt_no_origin", $('#pp_mt_no_origin').val());
        formData.append("bundle_unit", $('#pp_bundle_unit').val());
        formData.append("bundle_qty", $('#pp_bundle_qty').val());
        formData.append("sts_create", "copy");

        formData.append("stick", $('#pp_stick').val());
        formData.append("stick_unit", $('#pp_stick_unit').val());
        formData.append("price_unit", $('#pp_price_unit').val());
        formData.append("tot_price", $('#pp_tot_price').val());
        formData.append("price_least_unit", $('#pp_price_least_unit').val());
        formData.append("thick", $('#pp_thick').val());
        formData.append("thick_unit", $('#pp_thick_unit').val());
        formData.append("consum_yn", $('#pp_consum_yn').val());
        formData.append("barcode", $('#pp_barcode').val());

        if (files.length == 0) {
      
            formData.append("photo_file", $('#m_logo1').val());
        }
        $('#loading').show();
        $.ajax({
            type: 'POST',
            url: "/DevManagement/insertmaterial",
            data: formData,
            cache: false,
            contentType: false,
            processData: false,
            success: function (data) {
                $('#loading').hide();
                if (data.result == true) {
                    if (data.type != "PMT") {
                        var id = data.kq.mtid;
                        $("#list").jqGrid('addRowData', id, data.kq, 'first');
                        $("#list").setRowData(id, false, { background: "#d0e9c6" });
                    } else {
                        if (data.kq.Data.length == 0) { alert("MT Code already exists"); }
                        for (var i = 0; i < data.kq.Data.length; i++) {
                            var id = data.kq.Data[i].mtid;
                            $("#list").jqGrid('addRowData', id, data.kq.Data[i], 'first');
                            $("#list").setRowData(id, false, { background: "#d0e9c6" });
                        }
                    }
                    alert("Success");
                }
                else {
                    alert(data.kq);
                }
            },
            error: function (result) {
                $('.loading').hide();
                alert('The Code is the same. Please check again');
            }
        })
    }
});
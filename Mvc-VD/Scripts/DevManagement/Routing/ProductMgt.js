$("#list").jqGrid
    ({
        url: "/DevManagement/GetStyleMgt",
        datatype: 'json',
        mtype: 'Get',
        colModel: [
            { key: true, label: 'sid', name: 'sid', width: 80, align: 'center', hidden: true },
            { key: false, label: '', name: 'stamp_code', width: 80, align: 'center', hidden: true },

            { key: false, label: 'Product Code', name: 'style_no', width: 120, align: 'left' },
            { key: false, label: 'Product Name', name: 'style_nm', width: 120, align: 'left' },
            { key: false, label: 'Model', name: 'md_cd', sortable: true, width: 120, align: 'left' },
            { key: false, label: 'Project Name', name: 'prj_nm', sortable: true, width: '120', align: 'left' },
            { key: false, label: 'SS Version', name: 'ssver', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'Part Name', name: 'part_nm', editable: true, width: '120', align: 'left' },

            { key: false, label: 'Standard', name: 'standard', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'Customer Rev', name: 'cust_rev', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'Order Number', name: 'order_num', editable: true, width: '180', align: 'left', hidden: true },
            { key: false, label: 'Description', name: 'cav', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'Packing Amount(EA)', name: 'pack_amt', editable: true, width: '150', align: 'right', formatter: 'integer' },
            { key: false, label: 'Expiry Month', name: 'expiry_month', width: 80, align: 'center' },
            { key: false, label: 'Expiry', name: 'expiry', width: 150, align: 'center' },
            { key: false, label: 'Type', name: 'bom_type', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'TDS no', name: 'tds_no', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'QC Code', name: 'item_vcd', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'QC Range', name: 'qc_range_cd', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'QC Range', name: 'qc_range_cd', editable: true, width: '100px', align: 'left', hidden: true },
            { key: false, label: 'use_yn', name: 'use_yn', editable: true, width: '100px', hidden: true },
            { key: false, label: 'del_yn', name: 'del_yn', editable: true, width: '100px', hidden: true },

            { key: false, label: 'Create User', name: 'reg_id', index: 'reg_id', width: '70', align: 'left' },
            {
                key: false, label: 'Create Date', name: 'reg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '100'
            },
            //{ key: false, label: 'Change User', name: 'chg_id', editable: true, width: '100px', align: 'left' },
            //{ key: false, label: 'Change Date', name: 'chg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '200' },
        ],
        onSelectRow: function (rowid, selected, status, e) {
            var selectedRowId = $("#list").jqGrid("getGridParam", 'selrow');
            row_id = $("#list").getRowData(selectedRowId);
            $("#c_product").val(row_id.style_no);
            $("#tab_2").removeClass("active");
            $("#tab_1").addClass("active");
            $("#tab_c2").removeClass("active");
            $("#tab_c1").removeClass("hidden");
            $("#tab_c2").addClass("hidden");
            $("#tab_c1").addClass("active");
            $('#list2').clearGridData();
            $('#list2').jqGrid('setGridParam', { search: true });
           
            $('#listProcess').clearGridData();
            var pdata1 = $('#listProcess').jqGrid('getGridParam', 'postData');
            getData_listProcess(pdata1);
        },

        pager: jQuery('#gridpager'),
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        rownumbers: true,
        height: 250,
        width:null,
        autowidth: true,
        shrinkToFit: false,
        viewrecords: true,
        caption: "Product Information",
        sortable: true,
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

$('#list').jqGrid('setGridWidth', $("#two").width());
$(window).on("resize", function () {
    var newWidth = $("#list").closest(".ui-jqgrid").parent().width();
    $("#list").jqGrid("setGridWidth", newWidth, false);
});

$("#start").datepicker({ dateFormat: 'yy-mm-dd' }).val();
$("#end").datepicker({ dateFormat: 'yy-mm-dd' }).val();
$("#searchBtn").click(function () {
    var style_no = $('#style_no').val().trim();
    var style_nm = $('#style_nm').val().trim();
    var md_cd = $('#md_cd').val().trim();
    var prj_nm = $('#prj_nm').val().trim();
    var start = $('#start').val().trim();
    var end = $('#end').val().trim();
    $.ajax({
        url: "/DevManagement/searchStyle",
        type: "get",
        dataType: "json",
        data: {
            style_no: style_no,
            style_nm: style_nm,
            md_cd: md_cd,
            prj_nm: prj_nm,
            start: start,
            end: end,
        },
        success: function (result) {
            $("#list").jqGrid('clearGridData').jqGrid('setGridParam', { datatype: 'local', data: result }).trigger('reloadGrid');
            $('#listProcess').clearGridData();
          
        }
    });

});
function image_logo(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '<img src="../images/' + cellValue + '" style="height:20px;" />';
        return html;

    } else {
        return "";
    }
}
function bntCellValue(cellValue, options, rowdata, action) {
    id_delete = rowdata.sid;
    var html = '<button class="btn btn-xs btn-danger delete-btn" title="Delete" data-id_delete="' + rowdata.sid + '" >X</button>';
    return html;
};

function getData_primary(pdata) {
    var params = new Object();

    if ($('#list2').jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }

    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params._search = pdata._search;
    params.product = $("#c_product").val().trim();
    params.process_code = $("#c_process_code").val().trim();
    $('#list2').jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });

    $.ajax({
        url: `/DevManagement/Get_Rounting`,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
            if (st == "success") {
                var showing = $('#list2')[0];
                showing.addJSONData(data);
            }
        },
        error: function () {
            return;
        }
    });
};
$("#list2").jqGrid
    ({
        mtype: 'Get',
        colModel: [
            { label: 'idr', name: 'idr', index: 'idr',  hidden: true,key: true },
            { label: 'Product', name: 'style_no', sortable: true, width: '100', hidden: true },
            { label: 'Process', name: 'name',  hidden: true },
            { label: 'Level', name: 'level', width: '50', align: 'center' },
            { label: 'Process Name', name: 'name_pr', sortable: true, width: '150' },
            { label: 'Type', name: 'don_vi_pr', hidden:true },
            { label: 'Type', name: 'don_vi_prnm', index: 're_mark', width: '50', align: 'center' },
            { label: 'Description', name: 'description', width: '100', align: 'center' },
            { label: 'isFinish', name: 'isFinish', width: '50', align: 'center' },
            { label: 'User', name: 'reg_id', index: 'Reg Name', width: '50', align: 'center' },
            {
                key: false, label: 'Create Date', name: 'reg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '140'
            },
            //{ key: false, label: 'Change User', name: 'chg_id', editable: true, width: '100px', align: 'left' },
            //{ key: false, label: 'Change Date', name: 'chg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '150' },
        ],
        onSelectRow: function (rowid, selected, status, e) {
         
            var selectedRowId = $("#list2").jqGrid("getGridParam", 'selrow');
            row_id = $("#list2").getRowData(selectedRowId);
            $("#m_product").val(row_id.style_no);
            $("#mProcess").val(row_id.name);
            $("#mRoll").val(row_id.don_vi_pr);
            $("#idr").val(row_id.idr);
            $("#level").val(row_id.level);
            $("#m_description").val(row_id.description);
            
            $("#tab_1").removeClass("active");
            $("#tab_2").addClass("active");
            $("#tab_c1").removeClass("active");
            $("#tab_c2").removeClass("hidden");
            $("#tab_c1").addClass("hidden");
            $("#tab_c2").addClass("active");
            $("#m_save_but").attr("disabled", false);
            $("#del_save_but").attr("disabled", false);

            $('#list3').clearGridData();
            $('#list3').jqGrid('setGridParam', { search: true });
            var pdata = $('#list3').jqGrid('getGridParam', 'postData');
            getData_ProductCode(pdata);

            var isCheck = row_id.isFinish;
            if (isCheck == "Y") {
                document.getElementById("m_isFinish").checked = true;
            }
            else {
                document.getElementById("m_isFinish").checked = false;
            }
        },
        pager: '#grid2pager',
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        loadonce: false, //tải lại dữ liệu
        viewrecords: true,
        rownumbers: true,
        caption: 'Process',
        emptyrecords: 'No Data',
        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },
        height: 160,
        width: null,
        autowidth: true,
        shrinkToFit: false,
        multiselect: false,
   
    });
$("#tab_1").on("click", "a", function (event) {
    $("#tab_2").removeClass("active");
    $("#tab_1").addClass("active");
    $("#tab_c2").removeClass("active");
    $("#tab_c1").removeClass("hidden");
    $("#tab_c2").addClass("hidden");
    $("#tab_c1").addClass("active");
});

$("#tab_2").on("click", "a", function (event) {
    $("#tab_1").removeClass("active");
    $("#tab_2").addClass("active");
    $("#tab_c1").removeClass("active");
    $("#tab_c2").removeClass("hidden");
    $("#tab_c1").addClass("hidden");
    $("#tab_c2").addClass("active");
    $("#m_save_but").attr("disabled", true);
});
$("#tab_3").on("click", "a", function (event) {
    $("#tab_4").removeClass("active");
    $("#tab_3").addClass("active");
    $("#tab_c4").removeClass("active");
    $("#tab_c3").removeClass("hidden");
    $("#tab_c4").addClass("hidden");
    $("#tab_c3").addClass("active");
});

$("#tab_4").on("click", "a", function (event) {
    $("#tab_3").removeClass("active");
    $("#tab_4").addClass("active");
    $("#tab_c3").removeClass("active");
    $("#tab_c4").removeClass("hidden");
    $("#tab_c3").addClass("hidden");
    $("#tab_c4").addClass("active");
    $("#m_save_but").attr("disabled", true);
});
$("#tab_5").on("click", "a", function (event) {
    $("#tab_6").removeClass("active");
    $("#tab_5").addClass("active");
    $("#tab_c6").removeClass("active");
    $("#tab_c5").removeClass("hidden");
    $("#tab_c6").addClass("hidden");
    $("#tab_c5").addClass("active");
});

$("#tab_6").on("click", "a", function (event) {
    $("#tab_5").removeClass("active");
    $("#tab_6").addClass("active");
    $("#tab_c5").removeClass("active");
    $("#tab_c6").removeClass("hidden");
    $("#tab_c5").addClass("hidden");
    $("#tab_c6").addClass("active");
    $("#m_save_but").attr("disabled", true);
});
GetTIMSProcesses();
GetTIMSRolls();
function GetTIMSProcesses() {
    $.ajax({
        url: `/DevManagement/GetTIMSProcesses`
    })
        .done(function (response) {
            var html = `<option value="">*Select Process*</option>`;
            $.each(response, function (key, item) {
                    html += `<option value="${item.ProcessCode}">${item.ProcessName}</option>`;
            });
            $("#cProcess").html(html);
            $("#mProcess").html(html);
            return;
        })
        .fail(function () {
            return;
        });
}
function GetTIMSRolls() {
    var html = `<option value="">*Select Roll*</option>`;
    $.ajax({
        url: `/DevManagement/GetTIMSRolls`
    })
        .done(function (response) {
            if (response) {
                $.each(response, function (key, item) {
                    html += `<option value="${item.Code}">${item.Name}</option>`;
                });
            }
            $("#cRoll").html(html);
            $("#mRoll").html(html);
            return;
        })
        .fail(function () {
            $("#cRoll").html(html);
            $("#mRoll").html(html);
            return;
        });
}


$("#c_save_but").click(function () {
    if ($("#c_product").val() == "") {
        ErrorAlert("Vui lòng chọn Product Code");
        return false;
    }
    if ($("#c_process_code").val() == "") {
        ErrorAlert("Vui lòng chọn một công đoạn");
        return false;
    }
    if ($("#cProcess").val() == "") {
        ErrorAlert("Vui lòng chọn một công đoạn");
        return false;
    }
    if ($("#cProcess").val() == "") {
        ErrorAlert("Vui lòng chọn một đơn vị");
        return false;
    }
    else {
        var isFinish = "N";
        var y = $('#c_isFinish').is(':checked');
        if (y) {
            isFinish = "Y";
        }
       
        $.ajax({
            url: "/DevManagement/Create_Rounting",
            type: "get",
            dataType: "json",
            data: {
                style_no: $("#c_product").val().trim(),
                process_code: $("#c_process_code").val().trim(),
                name: $("#cProcess").val().trim(),
                don_vi_pr: $("#cRoll").val().trim(),
                description: $("#c_description").val().trim(),
                isFinish: isFinish,
            },
            success: function (response) {
                if (response.kq) {
                    SuccessAlert("");
                    var id = response.kq.idr;
                    $("#list2").jqGrid('addRowData', id, response.kq, 'first');
                    $("#list2").setRowData(id, false, { background: "#d0e9c6" });
                } else { 
                    ErrorAlert(response.message);
                }
            }
        });
    }
});


$("#m_save_but").click(function () {
    if ($("#m_product").val() == "") {
        alert("Please enter your Select Process on Table");
        return false;
    }
    else {
        var isFinish = "N";
        var y = $('#m_isFinish').is(':checked');
        if (y) {
            isFinish = "Y";
        }
        $.ajax({
            url: "/DevManagement/Modify_Rounting",
            type: "get",
            dataType: "json",
            data: {
                idr: $("#idr").val(),
                style_no: $("#m_product").val().trim(),
                name: $("#mProcess").val().trim(),
                don_vi_pr: $("#mRoll").val().trim(),
                description: $("#m_description").val().trim(),
                process_code: $("#c_process_code").val().trim(),
                isFinish: isFinish,
            },
            success: function (response) {
                if (response.kq) {
                    SuccessAlert("");
                    var id = response.kq.idr;
                    $("#list2").setRowData(id, response.kq, { background: "#d0e9c6" });
                    $("#list2").setRowData(id, false, { background: "#d0e9c6" });
                } else {
                    ErrorAlert(response.message);
                }
            }
        });
    }
});
$("#del_save_but").click(function () {
    $('#dialogDangerous').dialog('open');
});
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
        url: "/DevManagement/Deleteprocess",
        type: "post",
        dataType: "json",
        data: {
            idr: $("#idr").val(),
            process_code: $("#c_process_code").val(),
        },
        success: function (data) {
            if (data.result) {
                SuccessAlert("");
                id = $("#idr").val(),
                $("#list2").jqGrid('delRowData', id);
            }
            else {
                ErrorAlert(data.message);
            }
        },
        error: function (result) { }
    });
    $('#dialogDangerous').dialog('close');
});


$("#list3").jqGrid
    ({
        mtype: 'Get',
        colModel: [
            { key: true, label: 'id', name: 'id', width: 100, align: 'center', hidden: true},
            { name: '', label: 'Add', width: 50, align: 'center', formatter: AddMaterial, exportcol: false },
            { label: 'style_no', name: 'style_no', width: 150, align: 'left', hidden: true},
            { label: 'id_process', name: 'id_process', width: 150, align: 'left', hidden: true},
            { label: 'name', name: 'name', width: 150, align: 'left', hidden: true},
            { label: 'level', name: 'level', width: 150, align: 'left', hidden: true},
            { label: 'Material No', name: 'mt_no', width: 150, align: 'left' },
            { key: false, label: 'CAV', name: 'cav', sortable: true, width: '40', align: 'right', formatter: 'integer' },
            { key: false, label: 'Số lần sử dụng', name: 'need_time', sortable: true, width: '100', align: 'right', formatter: 'integer' },
            { key: false, label: 'Bước dập/Số đo (mm)', name: 'buocdap', sortable: true, width: '100', align: 'right', formatter: 'integer' },
            { key: false, label: 'Số Mét liệu cần để tạo ra 1EA(m)', name: 'need_m', sortable: true, width: '150', align: 'right', formatter: 'integer' },
            { label: 'MT Name', name: 'mt_nm', sortable: true, width: 150 },
            { key: false, label: 'Tính hiệu suất', name: 'isActive', width: 80, align: 'center' },
           
            { key: false, label: 'Create Date', name: 'reg_dt', align: 'center', width: 150, formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" } },

            //{ name: '', label: 'Delete', width: 100, align: 'center', formatter: DeleteBomMaterial, exportcol: false },
        ],
        onSelectRow: function (rowid, selected, status, e) {
            $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
            var selectedRowId = $("#list3").jqGrid("getGridParam", 'selrow');
            row_id = $("#list3").getRowData(selectedRowId);
         
            $("#tab_3").removeClass("active");
            $("#tab_4").addClass("active");
            $("#tab_c3").removeClass("active");
            $("#tab_c4").removeClass("hidden");
            $("#tab_c3").addClass("hidden");
            $("#tab_c4").addClass("active");

            $("#m_id").val(row_id.id);
            $("#m_mt_no").val(row_id.mt_no);
            $("#m_cav").val(row_id.cav);
            $("#m_need_time").val(row_id.need_time);
            $("#m_buocdap").val(row_id.buocdap);
            var isCheck = row_id.isActive;
            if (isCheck == "Y") {
                document.getElementById("m_isActive").checked = true;
            }
            else {
                document.getElementById("m_isActive").checked = false;
            }
         
        },
        pager: '#grid3pager',
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        loadonce: false, //tải lại dữ liệu
        viewrecords: true,
        rownumbers: true,
        caption: 'Material',
        emptyrecords: 'No Data',
        subGrid: true,
        subGridRowExpanded: showChildGridMaterial,

        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },
        height: 200,
        width: null,
        autowidth: true,
        shrinkToFit: false,
        multiselect: false,

    });
function getData_ProductCode(pdata) {
    var params = new Object();

    if ($('#list3').jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }

    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params._search = pdata._search;
    //params.level = $("#level").val().trim();
    params.name = $("#mProcess").val().trim();
    params.id_process = $("#idr").val().trim();
    params.product = $("#m_product").val().trim();
    params.process_code = $("#c_process_code").val().trim();
    $('#list3').jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });

    $.ajax({
        url: `/DevManagement/Get_ProductMaterial`,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
            if (st == "success") {
                var showing = $('#list3')[0];
                showing.addJSONData(data);
            }
        },
        error: function () {
            return;
        }
    });
};


$("#Create_Material").click(function () {
    if ($("#form3").valid() == true) {
        var materialNo = $("#c_mt_no").val().trim()
        var ProductCd = $("#c_product").val().trim()
        var id_process = $("#idr").val().trim()
        var level = $("#level").val().trim()
        var nameprocess = $("#mProcess").val().trim()
        var process_code = $("#c_process_code").val().trim()
        if (ProductCd == "" || ProductCd == undefined) {
            ErrorAlert("Vui lòng chọn PRODUCT");
            return;
        }
        if (process_code == "" || process_code == undefined) {
            ErrorAlert("Vui lòng chọn loại CÔNG ĐOẠN");
            return;
        }
        if (id_process == "" || id_process == undefined) {
            ErrorAlert("Vui lòng chọn CÔNG ĐOẠN");
            return;
        }
        if (level == "" || level == undefined) {
            ErrorAlert("Vui lòng chọn CÔNG ĐOẠN");
            return;
        }
        if (nameprocess == "" || nameprocess == undefined) {
            ErrorAlert("Vui lòng chọn CÔNG ĐOẠN");
            return;
        }
        if (materialNo == "" || materialNo == undefined ) {
            ErrorAlert("Vui lòng chọn NGUYÊN VẬT LIỆU");
            return;
        }
        var isActive = "N";
        var y = $('#c_isActive').is(':checked');
        if (y) {
            isActive = "Y";
        }
      
        var c_cav = $("#c_cav").val().trim();
        var c_need_time = $("#c_need_time").val().trim();
        var c_buocdap = $("#c_buocdap").val().trim();
       


        var settings = {
            "url": "/DevManagement/CreateProductMaterial",
            "method": "POST",
            "timeout": 0,
            "headers": {
                "Content-Type": "application/json"
            },
            "data": JSON.stringify({
                "mt_no": materialNo,
                "style_no": ProductCd,
                "cav": c_cav,
                "need_time": c_need_time,
                "buocdap": parseFloat(c_buocdap),
                "isActive": isActive,
                "level": level,
                "name": nameprocess,
                "process_code": process_code,
            }),
        };

        $.ajax(settings).done(function (resposive) {
            if (resposive.result) {
                SuccessAlert(resposive.message);
                var id = resposive.data.id;
                $("#list3").jqGrid('addRowData', id, resposive.data, 'first');
                $("#list3").setRowData(id, false, { background: "#d0e9c6" });
            }
            else {
                ErrorAlert(resposive.message);
            }
        });
       
    }
});


$("#Update_Material").click(function () {
    if ($("#form4").valid() == true) {
        var isActive = "N";
        var y = $('#m_isActive').is(':checked');
        if (y) {
            isActive = "Y";
        }
        $.ajax({
            type: "get",
            dataType: "json",
            url: "/DevManagement/ModifyProductMaterial",
            data: {
                id: $('#m_id').val(),
                mt_no: $("#m_mt_no").val(),
                cav: $("#m_cav").val(),
                need_time: $("#m_need_time").val(),
                buocdap: $("#m_buocdap").val(),
                isActive: isActive,
            },
            success: function (reposive) {
                if (reposive.result) {
                    SuccessAlert(reposive.message);
                    var id = reposive.ProductMaterial.id;
                    $("#list3").setRowData(id, reposive.ProductMaterial, { background: "#d0e9c6" });
                }
                else {
                    ErrorAlert(reposive.message);
                }
            },
            error: function (data) {
                ErrorAlert("Lỗi!!");
            }
        });
    }
});



$('#Delete_Material').click(function () {
    $.blockUI({ message: $('#question'), css: { width: '275px' } });
});

$('#yes').click(function () {
 
    $.blockUI({ message: "<h4>Đang xóa, hãy đợi...</h4>" });
  
    var settings = {
        "url": "/DevManagement/DeleteProductMaterial",
        "method": "POST",
        "timeout": 0,
        "headers": {
            "Content-Type": "application/json"
        },
        "data": JSON.stringify({
            "id": $('#m_id').val()

        }),
    };
    $.ajax(settings).done(function (reposive) {
        if (reposive.result) {
            SuccessAlert(reposive.message);
            $("#list3").jqGrid('delRowData', id);

            $('#list3').clearGridData();
            $('#list3').jqGrid('setGridParam', { search: true });
            var pdata = $('#list3').jqGrid('getGridParam', 'postData');
            getData_ProductCode(pdata);
            $.unblockUI();
        }
        else {
            ErrorAlert(reposive.message);
            $.unblockUI();
        }
    });
});

$('#no').click(function () {
    $.unblockUI();
    return false;
});


function AddMaterial(cellValue, options, rowdata, action) {
    return `<button  class="btn btn-sm btn-success button-srh"  data-style_no="${rowdata.style_no}"  data-id_process="${rowdata.id_process}" data-mt_no="${rowdata.mt_no}" onclick="AddMaterialOnClick(this)">+</button>`;
}
function AddMaterialOnClick(e) {
  
    $("#m_mt_no").val(e.dataset.mt_no);

    $('.dialog_addMATERIAL').dialog('open');
}
var parentRowKey2 = 0;
var childGridID2 = "";
function showChildGridMaterial(parentRowID, parentRowKey) {
    parentRowKey2 = parentRowKey;
    childGridID2 = parentRowID + "_table";
    var childGridPagerID = parentRowID + "_pager";
    $('#' + parentRowID).append('<table id=' + childGridID2 + '></table><div id=' + childGridPagerID + ' class=scroll></div>');

    var rowData = $("#list3").jqGrid('getRowData', parentRowKey);
    var process_code = $("#c_process_code").val().trim();

    $("#" + childGridID2).jqGrid({
        url: "/DevManagement/getMaterialChild?ProductCode=" + rowData.style_no + "&process_code=" + process_code + "&name=" + rowData.name + "&MaterialPrarent=" + encodeURIComponent(rowData.mt_no),
        mtype: "GET",
        datatype: "json",
        async: false,
        page: 1,
        colModel: [
            { key: true, label: 'id', name: 'id', width: 100, align: 'center', hidden: true },
            { label: 'MT Code', name: 'MaterialNo', width: 140 },
            { label: 'MT Name', name: 'MaterialName', sortable: true, width: 250 },
            { label: 'Create User', name: 'CreateId', sortable: true, width: 100 },
            { key: false, label: 'Create Date', name: 'CreateDate', align: 'center', width: 140, formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" } },

            { name: '', label: 'Delete', width: 100, align: 'center', formatter: DeleteMaterialChild, exportcol: false },

        ],
        onSelectRow: function (rowid, selected, status, e) {
            $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
            var selectedRowId = $("#list3").jqGrid("getGridParam", 'selrow');
            row_id = $("#list3").getRowData(selectedRowId);
            ProductCode = row_id.style_no;


        },
        shrinkToFit: false,
        loadonce: false,
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        width: null,
        height: '100%',
        subGrid: false, // set the subGrid property to true to show expand buttons for each row
        rownumbers: true,

        multiPageSelection: true,
        rowList: [50, 100, 200, 500, 1000],
        viewrecords: true,
        subGrid: false,

        pager: "#" + childGridPagerID,
        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },
        gridComplete: function () {

        }
    });
}
function DeleteMaterialChild(cellValue, options, rowdata, action) {

    return `<button  class="btn btn-sm btn-danger button-srh" data-id="${rowdata.id}" onclick="DeleteMaterialChildOnclick(this)">X</button>`;
}
function DeleteMaterialChildOnclick(e) {
    

    var id = e.dataset.id;
    var r = confirm("Bạn có chắc muốn xóa không?");
    if (r == true) {

        var settings = {
            "url": "/DevManagement/DeleteMaterialChild",
            "method": "POST",
            "timeout": 0,
            "headers": {
                "Content-Type": "application/json"
            },
            "data": JSON.stringify({
                "id": id

            }),
        };
        $.ajax(settings).done(function (data) {
            if (data.result) {
                SuccessAlert(data.message);
                $("#" + childGridID2).jqGrid('delRowData', id);

            }
            else {
                ErrorAlert(data.message);
            }
        });
    }
    else {
        return false;
    }
}

/// list process
$("#listProcess").jqGrid
    ({
        mtype: 'Get',
        colModel: [
            { key: true, label: 'id', name: 'id', width: 100, align: 'center', hidden: true },
            { label: 'Product', name: 'style_no', width: 100, align: 'left' },
            { label: 'process_code', name: 'process_code', width: 100, align: 'left', hidden: true },
            { label: 'Name', name: 'process_name', width: 150, align: 'left' },
            { label: 'Description', name: 'description', width: 150, align: 'left' },
            { label: 'Apply', name: 'IsApply', width: 50, align: 'center' },
            { key: false, label: 'Create User', name: 'reg_id', index: 'reg_id', width: '70', align: 'left' },
            {
                key: false, label: 'Create Date', name: 'reg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '150'
            },

        ],
        onSelectRow: function (rowid, selected, status, e) {
            $('.ui-state-highlight').css({ 'border': '#AAAAAA' });
            var selectedRowId = $("#listProcess").jqGrid("getGridParam", 'selrow');
            row_id = $("#listProcess").getRowData(selectedRowId);

            $("#tab_5").removeClass("active");
            $("#tab_6").addClass("active");
            $("#tab_c5").removeClass("active");
            $("#tab_c6").removeClass("hidden");
            $("#tab_c5").addClass("hidden");
            $("#tab_c6").addClass("active");

            $("#m_id_process").val(row_id.id);
            $("#c_process_code").val(row_id.process_code);
            $("#m_process_name").val(row_id.process_name);
            $("#m_description").val(row_id.description);
            var IsApply = row_id.IsApply;
            if (IsApply == "Y") {
                document.getElementById("m_IsApply").checked = true;
            }
            else {
                document.getElementById("m_IsApply").checked = false;
            }

            var pdata = $('#list2').jqGrid('getGridParam', 'postData');
            getData_primary(pdata);
            $('#list3').clearGridData();
        },
        pager: '#gridpagerProcess',
        rowNum: 50,
        rowList: [50, 100, 200, 500, 1000],
        loadonce: false, //tải lại dữ liệu
        viewrecords: true,
        rownumbers: true,
        caption: 'Product Process',
        emptyrecords: 'No Data',
       

        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },
        height: 200,
        width: null,
        autowidth: true,
        shrinkToFit: false,
        multiselect: false,

    });
function getData_listProcess(pdata) {
    var params = new Object();

    if ($('#listProcess').jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }

    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params._search = pdata._search;
    params.product = $("#c_product").val().trim();
  
    $('#listProcess').jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });

    $.ajax({
        url: `/DevManagement/Get_StyleProcess`,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
            if (st == "success") {
                var showing = $('#listProcess')[0];
                showing.addJSONData(data);
            }
        },
        error: function () {
            return;
        }
    });
};
$("#Create_Process").click(function () {
    if ($("#form5").valid() == true) {
        var ProductCd = $("#c_product").val().trim();
        var process_name = $("#c_process_name").val().trim();
        var description = $("#c_description").val().trim();
        if (ProductCd == "" || ProductCd == undefined) {
            ErrorAlert("Vui lòng chọn PRODUCT");
            return;
        }
        if (process_name == "" || process_name == undefined) {
            ErrorAlert("Vui lòng nhập tên công đoạn");
            return;
        }
        if (level == "" || level == undefined) {
            ErrorAlert("Vui lòng chọn CÔNG ĐOẠN");
            return;
        }
        
        var IsApply = "N";
        var y = $('#c_IsApply').is(':checked');
        if (y) {
            IsApply = "Y";
        }

        var settings = {
            "url": "/DevManagement/CreateProductProcess",
            "method": "POST",
            "timeout": 0,
            "headers": {
                "Content-Type": "application/json"
            },
            "data": JSON.stringify({
                "style_no": ProductCd,
                "process_name": process_name,
                "description": description,
                "IsApply": IsApply
            }),
        };

        $.ajax(settings).done(function (resposive) {
            if (resposive.result) {
                SuccessAlert(resposive.message);
                var id = resposive.data.id;
                $("#listProcess").jqGrid('addRowData', id, resposive.data, 'first');
                $("#listProcess").setRowData(id, false, { background: "#d0e9c6" });
            }
            else {
                ErrorAlert(resposive.message);
            }
        });

    }
});
$("#Update_Process").click(function () {
    if ($("#form6").valid() == true) {
        var IsApply = "N";
        var y = $('#m_IsApply').is(':checked');
        if (y) {
            IsApply = "Y";
        }
        $.ajax({
            type: "get",
            dataType: "json",
            url: "/DevManagement/ModifyProductProcess",
            data: {
                id: $('#m_id_process').val(),
                process_name: $("#m_process_name").val(),
                description: $("#m_description").val(),
                IsApply: IsApply,
            },
            success: function (reposive) {
                if (reposive.result) {
                    SuccessAlert(reposive.message);
                    var id = reposive.data.id;
                    $("#listProcess").setRowData(id, reposive.data, { background: "#d0e9c6" });
                }
                else {
                    ErrorAlert(reposive.message);
                }
            },
            error: function (data) {
                ErrorAlert("Lỗi!!");
            }
        });
    }
});
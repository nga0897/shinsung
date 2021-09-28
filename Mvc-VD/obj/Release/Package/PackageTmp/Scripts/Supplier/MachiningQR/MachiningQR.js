QCRange_data = [];

Item_vcd_data = [];

//$.ajax({
//    async: false,
//    url: "/DevManagement/GetQCRange_Marterial",
//    type: "get",
//    dataType: "json",
//    data: {},
//    success: function (data) {
//        $.each(data, function (key, item) {
//            QCRange_data.push(item);
//        });
//    },
//});

//$.ajax({
//    async: false,
//    url: "/DevManagement/Getpp_qc_type_mt",
//    type: "get",
//    dataType: "json",
//    data: {},
//    success: function (data) {
//        $.each(data, function (key, item) {
//            Item_vcd_data.push(item);
//        });
//    },
//});

$("#list").jqGrid
    ({
        mtype: 'GET',
        datatype: 'json',
        colModel: [
            { label: 'ID', name: 'mtid', key: true, width: 50, align: 'center', hidden: true },
            { label: 'Type', name: 'mt_type', sortable: true, width: 60, align: 'center', hidden: true },
            { label: 'Type', name: 'mt_type_nm', sortable: true, width: 100  },
            { label: 'Barcode', name: 'barcode', sortable: true, width: 80, align: 'center', hidden: true },
            { label: 'MT NO', name: 'mt_no', width: 150, align: 'left' },
            { label: 'Name', name: 'mt_nm', sortable: true, width: 450, },
            { label: 'Bundle Qty', name: 'bundle_qty', width: 80, align: 'right' },
            { label: 'Bundle Unit', name: 'bundle_unit', sortable: true, width: 80, align: 'left' },
            { label: 'Width (mm)', name: 'new_with', sortable: true, width: 80, align: 'right', formatter: widthformat },//////
            { label: 'Hidden', name: 'width', sortable: true, width: 80, align: 'right', hidden: true },
            { label: 'Hidden', name: 'width_unit', hidden: true, align: 'right' },
            { label: 'Length (M)', name: 'new_spec', sortable: true, width: 100, align: 'right', formatter: specformat },////
            { label: 'Hidden', name: 'spec', sortable: true, width: 100, align: 'right', hidden: true },
            { label: 'Hidden', name: 'spec_unit', width: 60, hidden: true, align: 'right' },
            { label: 'Supplier', name: 'sp_cd', sortable: true, width: 100, align: 'left', },
            { label: 'Manufactory', name: 'mf_cd', sortable: true, width: 150, align: 'left', hidden: true  },
            { label: 'Area(m²)', name: 'area_all', width: 80, align: 'right', formatter: areaformat },////
            { label: 'Hidden', name: 'area', width: 60, hidden: true, align: 'right' },
            { label: 'area_unit', name: 'area_unit', width: 60, hidden: true, align: 'right' },
            { label: 'Price', name: 'new_price', width: 80, align: 'right', formatter: priceformat },////
            { label: 'Hidden', name: 'price', width: 60, align: 'right', hidden: true },
            { label: 'Hidden', name: 'price_least_unit', width: 60, align: 'right', hidden: true },
            { label: 'Total Price ($)', name: 'tot_price_new', width: 80, align: 'right', formatter: totalpriceformat },////
            { label: 'Consumable', name: 'consumable', width: 150, align: 'center', hidden: true },////
            { label: 'Group Qty', name: 'gr_qty', sortable: true, width: 100, align: 'right', hidden: true},
            { label: 'Unit', name: 'unit_cd', sortable: true, width: 60, align: 'left', },
            { label: 'Hidden', name: 'tot_price', width: 60, align: 'right', hidden: true },
            { label: 'Hidden', name: 'price_unit', width: 60, hidden: true },
            { label: 'Stickness(g)', name: 'stick_new', width: 80, align: 'right', formatter: stickformat },///
            { label: 'Hidden', name: 'stick', width: 60, align: 'right', hidden: true },
            { label: 'Hidden', name: 'stick_unit', width: 60, align: 'right', hidden: true },
            { label: 'Thickness(µm)', name: 'thick_new', width: 80, align: 'right', formatter: thickformat },///
            { label: 'Hidden', name: 'thick', width: 60, align: 'right', hidden: true },
            { label: 'Hidden', name: 'thick_unit', width: 60, align: 'right', hidden: true },
            { label: 'Description', name: 're_mark', width: 130, cellattr: function (rowId, cellValue, rowObject) { return ' title="' + cellValue + '"'; }, hidden: true },
            { label: 'QC Code', name: 'item_vcd', sortable: true, width: 100, align: 'left', hidden: true },
            { label: 'QC Name', name: 'item_nm', sortable: true, width: 100, align: 'left', hidden: true },
            { label: 'QC Range', name: 'qc_range_cd_nm', sortable: true, width: 100, align: 'left', hidden: true },
            { label: 'Hidden', name: 'qc_range_cd', sortable: true, width: 100, align: 'left', hidden: true },
            { label: 'Origin MT NO', name: 'mt_no_origin', sortable: true, width: 100, align: 'left', },
            { label: 'Photo', name: 'photo_file', width: 100, formatter: downloadLink, align: 'center', hidden: true },
            { label: 'Photo', name: 'photo_file', hidden: true },
            { label: 'Create Date', name: 'reg_dt', width: 100, align: 'center', formatter: "date", formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d" } },
            { label: 'Create User', name: 'reg_id', width: 90, align: 'left' },
            { label: 'Change Name', name: 'chg_id', width: 90, align: 'left', hidden: true },
            { label: 'Change Date', name: 'chg_dt', width: 100, align: 'center', formatter: "date", formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d" }, hidden: true },
        ],
        cmTemplate: { title: false },
        gridComplete: function () {
            var rows = $("#list").getDataIDs();
            for (var i = 0; i < rows.length; i++) {
                var use_yn = $("#list").getCell(rows[i], "barcode");
                if (use_yn == "Y") {
                    $("#list").jqGrid('setRowData', rows[i], false, { background: 'rgb(241 206 151)' });
                }
            }
        },
        onSelectRow: function (rowid, status, e) {
            var selectedRowId = $("#list").jqGrid("getGridParam", 'selrow');
            row_id = $("#list").getRowData(selectedRowId);
            //$("#list3").setGridParam({ url: "/Supplier/getdatalist3_Supplier_QR_Management?" + "&mt_no=" + row_id.mt_no, datatype: "json" }).trigger("reloadGrid");
            var mtid = row_id.mtid;
            var mt_type = row_id.mt_type;
            var mt_no = row_id.mt_no;
            var mt_nm = row_id.mt_nm;
            var width = row_id.width;
            var width_unit = row_id.width_unit;
            var spec = row_id.spec;
            var spec_unit = row_id.spec_unit;
            var price = row_id.price;
            var price_unit = row_id.price_unit;
            var re_mark = row_id.re_mark;
            var photo_file = row_id.photo_file;

            $('#m_bundle_qty').val(row_id.bundle_qty);
            $('#lot_mt_no').val(row_id.mt_no);
            $('#m_stick').val(row_id.stick);
            $('#m_stick_unit').val(row_id.stick_unit);
            $('#m_price_unit').val(row_id.price_unit);
            $('#m_tot_price').val(row_id.tot_price);
            $('#m_price').val(row_id.price);
            $('#m_price_least_unit').val(row_id.price_least_unit);
            //$('#m_mt_cd').val(row_id.mt_cd);
            $('#m_thick').val(row_id.thick);
            $('#m_thick_unit').val(row_id.thick_unit);

            $('#m_barcode').val(row_id.barcode);

            $('#m_unit_cd').val(row_id.unit_cd);
            $('#m_bundle_unit').val(row_id.bundle_unit);
            $('#m_mt_no_origin').val(row_id.mt_no_origin);
            $('#m_area').val(row_id.area);
            $('#m_s_lot_no').val(row_id.s_lot_no);
            $('#m_area_unit').val(row_id.area_unit);
            $('#m_item_vcd').val(row_id.item_vcd);
            $('#m_qc_range').val(row_id.qc_range_cd);
            $('#m_logo1').val(photo_file);
            $('#m_manufac').val(row_id.mf_cd);
            $('#m_supllier').val(row_id.sp_cd);
            $('#m_mtid').val(mtid);
            $('#m_mt_no').val(mt_no);
            $('#m_mt_nm').val(mt_nm);
            $('#m_width').val(width);
            $('#m_width_unit').val(width_unit);
            $('#m_spec').val(spec);
            $('#m_spec_unit').val(spec_unit);

            $('#m_re_mark').val(re_mark);
            $('#m_gr_qty').val(row_id.gr_qty);
            $('#m_consumable').val(row_id.consumable);

            if (photo_file != null) {
                $("#m_logo").html('<img src="../images/MarterialImg/' + photo_file + '" style="height:50px" />');

            } else {
                $("#m_logo").html("");
            }


            $("#tab_1").removeClass("active");
            $("#tab_2").addClass("active");
            $("#tab_c1").removeClass("active");
            $("#tab_c2").removeClass("hidden");
            $("#tab_c1").addClass("hidden");
            $("#tab_c2").addClass("active");

            $("#m_save_but").removeClass("hidden");
            $("#c_save_but").removeClass("active");
            $("#m_save_but").addClass("active");
            $("#c_save_but").addClass("hidden");
            $("#m_save_but").attr("disabled", false);
            if (row_id.mt_type == 'MMT') {
                $("#pp_save_but").attr("disabled", false);
            } else {
                $("#pp_save_but").attr("disabled", true);
            }


            //copy mt
            $('#pp_stick').val(row_id.stick);
            $('#pp_stick_unit').val(row_id.stick_unit);
            $('#pp_price_unit').val(row_id.price_unit);
            $('#pp_tot_price').val(row_id.tot_price);
            $('#pp_price').val(row_id.price);
            $('#pp_price_least_unit').val(row_id.price_least_unit);
            //$('#pp_mt_cd').val(row_id.mt_cd);
            $('#pp_thick').val(row_id.thick);
            $('#pp_thick_unit').val(row_id.thick_unit);

            $('#pp_unit_cd').val(row_id.unit_cd);
            $('#pp_bundle_qty').val(row_id.bundle_qty);
            $('#pp_bundle_unit').val(row_id.bundle_unit);
            var mt_origin = row_id.mt_no_origin;
            if (mt_origin != "") {
                $('#pp_mt_no_origin').val(row_id.mt_no_origin);
            }
            else {
                $('#pp_mt_no_origin').val(mt_no);
            }
            $('#pp_area').val(row_id.area);
            $('#pp_s_lot_no').val(row_id.s_lot_no);
            $('#pp_area_unit').val(row_id.area_unit);
            $('#pp_item_vcd').val(row_id.item_vcd);
            $('#pp_qc_range').val(row_id.qc_range_cd);
            $('#pp_logo1').val(photo_file);
            $('#pp_manufac').val(row_id.mf_cd);
            $('#pp_supllier').val(row_id.sp_cd);
            $('#pp_mtid').val(mtid);
            $('#pp_mt_type').val(row_id.mt_type);
            $('#pp_mt_no').val(mt_no);
            $('#pp_mt_nm').val(mt_nm);
            $('#pp_width').val(width);
            $('#pp_width_unit').val(width_unit);
            //$('#m_use_yn').val(use_yn);
            //$('#m_del_yn').val(del_yn);
            $('#pp_spec').val(spec);
            $('#pp_spec_unit').val(spec_unit);

            $('#pp_re_mark').val(re_mark);
            $('#pp_gr_qty').val(row_id.gr_qty);
            $('#pp_consum_yn').val(row_id.consumable);

            if (photo_file != null) {
                $("#m_logo1").html('<img src="../images/MarterialImg/' + photo_file + '" style="height:50px" />');

            } else {
                $("#m_logo1").html("");
            }
            $('#m_mt_type').val(row_id.mt_type);
        },

        viewrecords: true,
        height: 400,
        rowNum: 50,
        rownumbers: true,      //컬럼 맨 앞에 순번컬럼 붙일지 말지( 1,2,3...)
        rowList: [50, 100, 200, 500, 1000], //한 번에 보여줄 레코드 수를 변동할 때 선택 값
        // reload 여부이면 true로 설정하며 한번만 데이터를 받아오고 그 다음부터는 데이터를 받아오지 않음
        loadtext: "Loading...",			// 서버연동시 loading 중이라는 표시에 문자열 지정
        emptyrecords: "No data.",	// 데이터가 없을 경우 보열줄 문자열 
        gridview: true,
        shrinkToFit: false,
        datatype: function (postData) { getDataOutBox(postData); },
        pager: "#jqGridPager",
        jsonReader:
        {
            root: "rows",
            page: "page",
            total: "total",
            records: "records",
            repeatitems: false,
            Id: "0"
        },
        ajaxGridOptions: { contentType: "application/json" },
        autowidth: true,
    });

$('#list').jqGrid('setGridWidth', $(".boxlist").width());

function getDataOutBox(pdata) {
    $('.loading').show();

    var type = $("#s_mt_type").val().trim();
    var code = $("#mt_no").val().trim();
    var name = $("#mt_nm").val().trim();
    var start1 = $("#start1").val();
    var end1 = $("#end1").val();
    var params = new Object();
    if (jQuery('#list').jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }
    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params.type = type;
    params.code = code;
    params.name = name;
    params.start = start1;
    params.end = end1;
    //params.end1Data = type;
    $("#list").jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });
    params._search = pdata._search;

    $.ajax({
        url: '/Supplier/searchMachiningQR',
        type: "Get",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
            if (st == "success") {
                var grid = $("#list")[0];
                grid.addJSONData(data);
                $('.loading').hide();
            }
        }
    })
};


$("#searchBtn").click(function () {
    $("#list").clearGridData();
    $('.loading').show();
    var grid = $("#list");
    grid.jqGrid('setGridParam', { search: true });
    var pdata = grid.jqGrid('getGridParam', 'postData');
    getDataOutBox(pdata);
});

$(window).on("resize", function () {
    var newWidth = $("#list").closest(".ui-jqgrid").parent().width();
    $("#list").jqGrid("setGridWidth", newWidth, false);
});

function formatNumber(nStr, decSeperate, groupSeperate) {
    nStr += '';
    x = nStr.split(decSeperate);
    x1 = x[0];
    x2 = x.length > 1 ? '.' + x[1] : '';
    var rgx = /(\d+)(\d{3})/;
    while (rgx.test(x1)) {
        x1 = x1.replace(rgx, '$1' + groupSeperate + '$2');
    }
    return x1 + x2;
}

function widthformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.width_unit != null) ? rowdata.width_unit : "");
        return html;

    } else {
        return "";
    }
}

function specformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {

        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.spec_unit != null) ? rowdata.spec_unit : "");
        return html;

    } else {
        return "";
    }
}

function areaformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {

        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.area_unit != null) ? rowdata.area_unit : "");
        return html;

    } else {
        return "";
    }
}

function priceformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.price_least_unit != null) ? rowdata.price_least_unit : "");
        return html;

    } else {
        return "";
    }
}

function totalpriceformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.price_unit != null) ? rowdata.price_unit : "");
        return html;

    } else {
        return "";
    }
}

function stickformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.stick_unit != null) ? rowdata.stick_unit : "");
        return html;

    } else {
        return "";
    }
}

function thickformat(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = formatNumber(cellValue, '.', ',') + " " + ((rowdata.thick_unit != null) ? rowdata.thick_unit : "");
        return html;

    } else {
        return "";
    }
}

function qc_range_nm_format(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '';
        $.each(QCRange_data, function (key, item) {
            if (cellValue == item.dt_cd) { html += item.dt_nm; }
        });
        return html;
    }
    else {
        return "";
    }
}

function item_vcd_nm_format(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '';
        $.each(Item_vcd_data, function (key, item) {
            if (cellValue == item.item_vcd) { html += item.item_nm; }
        });
        return html;
    }
    else {
        return "";
    }
}


function downloadLink(cellValue, options, rowdata, action) {
    if (cellValue != null) {
        var html = '<a href="#" class="popupimg" data-img="' + cellValue + '"><img src="../Images/MarterialImg/' + cellValue + '" style="height:20px;" /></a>';
        return html;

    } else {
        return "";
    }
};





//select option

$("#start1").datepicker({ dateFormat: 'yy-mm-dd' }).val();
$("#end1").datepicker({ dateFormat: 'yy-mm-dd' }).val();

//GetType_Marterial();
function GetType_Marterial() {
    $.get("/DevManagement/GetType_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '<option value="">*Type*</option>';
            html += '<option value=' + data[0].dt_cd + '>' + data[0].dt_nm + '</option>';
            for (var i = 1; i < data.length; i++) {
                html += '<option value=' + data[i].dt_cd + '>' + data[i].dt_nm + '</option>';
            }
            $(".gettype").html(html);
        }


    });
};

//GetWidth_Marterial();
function GetWidth_Marterial() {
    $.get("/DevManagement/GetWidth_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $(".getwidth").empty();
            $.each(data, function (key, item) {
                if (item.dt_nm == "mm") { html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>'; } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $(".getwidth").html(html);
        }
    });
};

//GetSpec_Marterial();
function GetSpec_Marterial() {
    $.get("/DevManagement/GetSpec_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $(".getspec").empty();
            $.each(data, function (key, item) {
                if (item.dt_nm == "M") { html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>'; } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $(".getspec").html(html);
        }
    });
};

//_GetUnit_qty();
function _GetUnit_qty() {

    $.get("/DevManagement/GetUnit_qty", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $.each(data, function (key, item) {
                if (item.dt_nm == "M") {
                    html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>';
                } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $("#c_unit_cd").html(html);
            $("#m_unit_cd").html(html);
            $("#pp_unit_cd").html(html);
        }
    });
}

//GetPrice_Marterial();
function GetPrice_Marterial() {
    $.get("/DevManagement/GetPrice_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $(".getprice").empty();
            $.each(data, function (key, item) {
                if (item.dt_nm == "USD") {
                    html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>';
                } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $(".getprice").html(html);
        }
    });
};

//GetArea_Marterial();
function GetArea_Marterial() {
    $.get("/DevManagement/GetArea_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $.each(data, function (key, item) {
                if (item.dt_nm == "USD") { html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>'; } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $("#c_thick_unit").html(html);
            $("#m_thick_unit").html(html);
            $("#pp_thick_unit").html(html);

        }
    });
}

//GetStickness();
function GetStickness() {
    $.get("/DevManagement/GetStickness", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $.each(data, function (key, item) {
                if (item.dt_nm == "g") { html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>'; } else {
                    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
                }
            });
            $("#c_stick_unit").html(html);
            $("#m_stick_unit").html(html);
            $("#pp_stick_unit").html(html);

        }
    });
}

//Getprice_least_unit();
function Getprice_least_unit() {
    $.get("/DevManagement/Getprice_least_unit", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            $.each(data, function (key, item) {
                if (item.dt_nm == "/M2") { html += '<option value=' + item.dt_cd + ' selected="selected">' + item.dt_nm + '</option>'; } else { html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>'; }

            });
            $("#c_price_least_unit").html(html);
            $("#m_price_least_unit").html(html);
            $("#pp_price_least_unit").html(html);

        }
    });
}

//qc range
//_GetQCRange();
function _GetQCRange() {
    $.get("/DevManagement/GetQCRange_Marterial", function (data) {
        if (data != null && data != undefined && data.length) {
            var html = '';
            html += '<option value="" selected="selected">*QC Range*</option>';
            $.each(data, function (key, item) {
                html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
            });
            $("#qc_range").html(html);
            $("#m_qc_range").html(html);
            $("#pp_qc_range").html(html);
            $("#qc_range").val("SOM");
            $("#m_qc_range").val("SOM");;
            $("#pp_qc_range").val("SOM");


        }
    });
}

//get bundle unit
_Getbundle();
function _Getbundle() {
    $.get("/DevManagement/Get_Getbundle", function (data) {
        if (data != null && data != undefined && data.length) {
            //var html = '';
            //html += '<option value="" selected="selected">*Bundle Unit*</option>';
            //$.each(data, function (key, item) {
            //    html += '<option value=' + item.dt_cd + '>' + item.dt_nm + '</option>';
            //});
            //$("#c_bundle_unit").html(html);
            //$("#m_bundle_unit").html(html);
            //$("#pp_bundle_unit").html(html);

            var html = '';
            $("#c_bundle_unit").empty();
            $("#m_bundle_unit").empty();
            $("#pp_bundle_unit").empty();

            html += '<option value=' + data[0].dt_cd + '>' + data[0].dt_nm + '</option>';
            for (var i = 1; i < data.length; i++) {
                html += '<option value=' + data[i].dt_cd + '>' + data[i].dt_nm + '</option>';
            }
            $("#c_bundle_unit").html(html);
            $("#m_bundle_unit").html(html);
            $("#pp_bundle_unit").html(html);
        }
    });
}

function hamtinhtoan() {
    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('c_width').value == "") ? 0 : document.getElementById('c_width').value;
    var width_unit = (document.getElementById('c_width_unit').value == "") ? 0 : document.getElementById('c_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('c_spec').value == "") ? 0 : document.getElementById('c_spec').value;
    document.getElementById("c_price").value = (document.getElementById('c_price').value == "") ? 0 : document.getElementById('c_price').value;
    document.getElementById("c_spec").value = (document.getElementById('c_spec').value == "") ? 0 : document.getElementById('c_spec').value;
    document.getElementById("c_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('c_price').value == "") ? 0 : document.getElementById('c_price').value;

    var Area = (document.getElementById('c_area').value == "") ? 0 : document.getElementById('c_area').value;

    document.getElementById("c_tot_price").value = Math.round((parseFloat(price * Area)));
}

function hamtinhtoan1() {
    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('m_width').value == "") ? 0 : document.getElementById('m_width').value;
    var width_unit = (document.getElementById('m_width_unit').value == "") ? 0 : document.getElementById('m_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('m_spec').value == "") ? 0 : document.getElementById('m_spec').value;
    document.getElementById("m_price").value = (document.getElementById('m_price').value == "") ? 0 : document.getElementById('m_price').value;
    document.getElementById("m_spec").value = (document.getElementById('m_spec').value == "") ? 0 : document.getElementById('m_spec').value;
    document.getElementById("m_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('m_price').value == "") ? 0 : document.getElementById('m_price').value;

    var Area = (document.getElementById('m_area').value == "") ? 0 : document.getElementById('m_area').value;

    document.getElementById("m_tot_price").value = Math.round((parseFloat(price * Area)));



}

function hamtinhtoan2() {
    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('pp_width').value == "") ? 0 : document.getElementById('pp_width').value;
    var width_unit = (document.getElementById('pp_width_unit').value == "") ? 0 : document.getElementById('pp_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('pp_spec').value == "") ? 0 : document.getElementById('pp_spec').value;
    document.getElementById("pp_price").value = (document.getElementById('pp_price').value == "") ? 0 : document.getElementById('pp_price').value;
    document.getElementById("pp_spec").value = (document.getElementById('pp_spec').value == "") ? 0 : document.getElementById('pp_spec').value;
    document.getElementById("pp_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('pp_price').value == "") ? 0 : document.getElementById('pp_price').value;

    var Area = (document.getElementById('pp_area').value == "") ? 0 : document.getElementById('pp_area').value;

    document.getElementById("pp_tot_price").value = Math.round((parseFloat(price * Area)));



}


function sumact_gr_qty() {

    var gr_qty = (document.getElementById('c_gr_qty').value == "") ? 0 : document.getElementById('c_gr_qty').value;


    if (gr_qty > 0) {

        document.getElementById("c_spec").value = parseFloat(gr_qty);
    }
    else {
        document.getElementById("c_spec").value = "";
    }

    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('c_width').value == "") ? 0 : document.getElementById('c_width').value;
    var width_unit = (document.getElementById('c_width_unit').value == "") ? 0 : document.getElementById('c_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('c_spec').value == "") ? 0 : document.getElementById('c_spec').value;
    document.getElementById("c_price").value = (document.getElementById('c_price').value == "") ? 0 : document.getElementById('c_price').value;
    document.getElementById("c_spec").value = (document.getElementById('c_spec').value == "") ? 0 : document.getElementById('c_spec').value;
    document.getElementById("c_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('c_price').value == "") ? 0 : document.getElementById('c_price').value;

    var Area = (document.getElementById('c_area').value == "") ? 0 : document.getElementById('c_area').value;

    document.getElementById("c_tot_price").value = Math.round((parseFloat(price * Area)));

}
function sumact_gr_qty1() {

    var gr_qty = (document.getElementById('m_gr_qty').value == "") ? 0 : document.getElementById('m_gr_qty').value;


    if (gr_qty > 0) {

        document.getElementById("m_spec").value = parseFloat(gr_qty);
    }
    else {
        document.getElementById("m_spec").value = "";
    }

    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('m_width').value == "") ? 0 : document.getElementById('m_width').value;
    var width_unit = (document.getElementById('m_width_unit').value == "") ? 0 : document.getElementById('m_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('m_spec').value == "") ? 0 : document.getElementById('m_spec').value;
    document.getElementById("m_price").value = (document.getElementById('m_price').value == "") ? 0 : document.getElementById('m_price').value;
    document.getElementById("m_spec").value = (document.getElementById('m_spec').value == "") ? 0 : document.getElementById('m_spec').value;
    document.getElementById("m_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('m_price').value == "") ? 0 : document.getElementById('m_price').value;

    var Area = (document.getElementById('m_area').value == "") ? 0 : document.getElementById('m_area').value;

    document.getElementById("m_tot_price").value = Math.round((parseFloat(price * Area)));

}
function sumact_gr_qty2() {

    var gr_qty = (document.getElementById('pp_gr_qty').value == "") ? 0 : document.getElementById('pp_gr_qty').value;


    if (gr_qty > 0) {

        document.getElementById("pp_spec").value = parseFloat(gr_qty);
    }
    else {
        document.getElementById("pp_spec").value = "";
    }

    //từ mm đổi ra m 
    var gtri = 0;

    var width = (document.getElementById('pp_width').value == "") ? 0 : document.getElementById('pp_width').value;
    var width_unit = (document.getElementById('pp_width_unit').value == "") ? 0 : document.getElementById('pp_width_unit').value;
    if (width_unit == "mm") {
        width = width / 1000.0;
    }
    if (width_unit == "cm") {
        width = width / 100.00;
    }
    var Length = (document.getElementById('pp_spec').value == "") ? 0 : document.getElementById('pp_spec').value;
    document.getElementById("pp_price").value = (document.getElementById('pp_price').value == "") ? 0 : document.getElementById('pp_price').value;
    document.getElementById("pp_spec").value = (document.getElementById('pp_spec').value == "") ? 0 : document.getElementById('pp_spec').value;
    document.getElementById("pp_area").value = Math.round((parseFloat(width * Length)));

    var price = (document.getElementById('pp_price').value == "") ? 0 : document.getElementById('pp_price').value;

    var Area = (document.getElementById('pp_area').value == "") ? 0 : document.getElementById('pp_area').value;

    document.getElementById("pp_tot_price").value = Math.round((parseFloat(price * Area)));

}




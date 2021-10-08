$(document).ready(function () {

    SetDatePicker();

   // GetProduct();

   // ProductOnChangeEvent();

    ShowGrid();

    $(`#createBtn`).attr("disabled", true);
});

//var stampCode = undefined;
var stampCode = $(`#stampCode`).val();
function ProductOnChangeEvent() {
    document.getElementById("productCode").addEventListener("change", function () {
        var product = $(`#productCode`).val();
        $.get(`/DevManagement/GetProduct?product=${product}`, function (response) {
            if (response.data) {
                $(`#quantityPerTray`).val(response.data[0].pack_amt);
                stampCode = response.data[0].stamp_code;
                $(`#createBtn`).attr("disabled", false);
            }
            else {
                $(`#createBtn`).attr("disabled", true);
            }
        });
    });
};

function SetDatePicker() {
    $(`#printedDate`).datepicker({
        showButtonPanel: true,
        //minDate: new Date(),
        dateFormat: 'yy-mm-dd'
    }).datepicker("setDate", new Date());
};

function GetProduct() {
    $.get(`/DevManagement/GetProduct?product=`, function (response) {
        if (response.data) {
            var html = `<option value="">*Select Product*</option>`;
            $.each(response.data, function (key, item) {
                html += `<option value="${item.style_no}">${item.style_no}</option>`
            });
            $(`#productCode`).html(html);
        }
    });
};

$(`#createBtn`).on(`click`, function () {


  

     stampCode = $(`#stampCode`).val();
    var productCode = $(`#productCode`).val() == null ? `` : $(`#productCode`).val().trim();
    var vendorCode = $(`#vendorCode`).val() == null ? `` : $(`#vendorCode`).val().trim();
    var vendorLine = $(`#vendorLine`).val() == null ? `` : $(`#vendorLine`).val().trim();
    var labelPrinter = $(`#labelPrinter`).val() == null ? `` : $(`#labelPrinter`).val().trim();
    var type = $(`#type`).val() == null ? `` : $(`#type`).val().trim();
    var pcn = $(`#pcn`).val() == null ? `` : $(`#pcn`).val().trim();
    var printedDate = $(`#printedDate`).val() == null ? `` : $(`#printedDate`).val().trim();
    var machineLine = $(`#machineLine`).val() == null ? `` : $(`#machineLine`).val().trim();
    var shift = $(`#shift`).val() == null ? `` : $(`#shift`).val().trim();
    var quantity = $(`#quantity`).val() == null ? `` : $(`#quantity`).val().trim();
    var quantityPerTray = $(`#quantityPerTray`).val() == null ? `` : $(`#quantityPerTray`).val().trim();
    var ssver = $(`#c_ssver`).val() == null ? `` : $(`#c_ssver`).val().trim();
   
    if (!quantity) {
        ErrorAlert(`Vui lòng nhập số lượng cần in.`);
        return;
    }
    if (parseInt(quantity)> 200) {
        ErrorAlert(`Số lượng cho phép in tem tối đa là 200 tem`);
        return;
    }
    if (!productCode) {
        ErrorAlert(`Vui lòng chọn mã sản phẩm.`);
        return;
    }
    //kiểm tra cột version có null không nếu null thì nhắc nhơ
    if (ssver == "") {
        var r = confirm("Version bạn chưa nhập dữ liệu,Bạn có muốn tiếp tục tạo tem !!!");
        if (r == false) {
            return false
        }
    }
    var r = confirm("Bạn đồng ý tạo tem với các thông tin đã nhập, bấm OK để tạo tem !!!");
    if (r == false) {
        return false
    }
    $(`#loading`).show();

    $.ajax({
        type: "GET",
        url: `/CreateBuyerQR/Create?printedDate=${printedDate}`,
        data: {
            productCode: productCode,
            vendorCode: vendorCode,
            vendorLine: vendorLine,
            labelPrinter: labelPrinter,
            type: type,
            pcn: pcn,
            machineLine: machineLine,
            shift: shift,
            quantity: quantity,
            quantityPerTray: quantityPerTray,
            stampCode: stampCode,
            ssver: ssver
        },
        contentType: "application/json; charset=utf-8",
        async: true,
        cache: false
    })
        .done(function (response) {
            $(`#loading`).hide();
            if (response.result) {
                
                grid.jqGrid('clearGridData').jqGrid('setGridParam', { datatype: 'local', data: response.data, background: "#d0e9c6" }).trigger("reloadGrid");

                //for (var item of response.data) {
                //    grid.jqGrid('addRowData', item.id, item, 'first');
                //    grid.setRowData(item.id, false, { background: "#d0e9c6" });
                //}
                SuccessAlert(`Tạo mã tem thành công.`);
                return;
            }
            else {
                ErrorAlert(response.message);
                return;
            }
        })
        .fail(function (response) {
            $(`#loading`).hide();
            ErrorAlert(`Lỗi hệ thống.`);
            return;
        });
});

//GRID
var grid = $("#buyerQRGrid");
function ShowGrid() {
    grid.jqGrid({
        mtype: 'GET',
        datatype: function (postData) { getData(postData); },
        colModel: [
            { name: 'id', key: true, hidden: true },
            { name: 'buyer_qr', label: 'Tem Code', width: 300, align: 'left',hidden:false },
            { name: 'product_code', label: 'Product', width: 150, align: 'left' },
            { name: 'product_name', label: 'Name', width: 150, align: 'left' },
            { name: 'lotNo', label: 'Lot No.', width: 100, align: 'center' },
            { name: 'model', label: 'Model', width: 150, align: 'center' },
            { name: 'quantity', label: 'Quy cách đóng gói (EA)', width: 150, align: 'center' },
            { name: 'stamp_name', label: 'Loại tem', width: 100, align: 'center' },
            {
                key: false, label: 'Create Date', name: 'reg_dt', editable: true, align: 'center', formatter: 'date', formatoptions: { srcformat: "ISO8601Long", newformat: "Y-m-d H:i:s" }, width: '130'
            },
        ],
        pager: '#buyerQRPager',
        viewrecords: true,
        rowNum: 1000,
        rowList: [1000, 2000, 3000,4000,5000, 10000],
        sortable: true,
        loadonce: false,
        height: 500,
        width: null,
        autowidth: false,
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
};

$("#printQRBtn").click(function () {
    //var i, selRowIds = grid.jqGrid("getGridParam", "selarrrow"), n, rowData;

    //if (selRowIds.length > 0) {
    //    var prd_cd = $("#cp_style_no").val();
    //    window.open("/CreateBuyerQR/PrintQR?id=" + selRowIds , "PRINT", "width=600, height=800, left=200, top=100, location=no, status=no")
    //}
    //else {
    //    ErrorAlert("Vui lòng chọn trên bảng");
    //}

    var fruits = [];
    var i, selRowIds = grid.jqGrid("getGridParam", "selarrrow"), n, rowData;
    for (i = 0, n = selRowIds.length; i < n; i++) {
        var ret = grid.jqGrid('getRowData', selRowIds[i]);
      
        var id = ret.id
        fruits.push(id);
    };
    if (selRowIds.length > 0) {
     
        var mapForm = document.createElement("form");
        mapForm.target = "Map";
        mapForm.type = "hidden";
        mapForm.method = "POST"; // or "post" if appropriate
        mapForm.action = "/CreateBuyerQR/PrintQR";

        var mapInput = document.createElement("input");
        mapInput.type = "hidden";
        mapInput.name = "id";
        mapInput.value = fruits;
        mapForm.appendChild(mapInput);
        //0
        document.body.appendChild(mapForm);

        map = window.open(" ", "Map", "width=600, height=800, left=200 top=100, location=no, status=no,");

        if (map) {
            mapForm.submit();
        }

    }
    else {
        alert("Vui Lòng chọn Mã Cần In!!.");
    }



});

$("#searchQRBtn").click(function () {
    grid.clearGridData();
    grid.jqGrid('setGridParam', { search: true });
    var pdata = grid.jqGrid('getGridParam', 'postData');
    getData(pdata);
});
function getData(pdata) {
    var params = new Object();

    if (grid.jqGrid('getGridParam', 'reccount') == 0) {
        params.page = 1;
    }
    else { params.page = pdata.page; }

    params.rows = pdata.rows;
    params.sidx = pdata.sidx;
    params.sord = pdata.sord;
    params._search = pdata._search;

   
    params.product = $("#productCode").val().trim();
    params.printedDate = $("#printedDate").val().trim();
    params.shift = $("#shift").val().trim();
  


    grid.jqGrid('setGridParam', { search: true, postData: { searchString: $("#auto_complete_search").val() } });

    $.ajax({
        url: `/TIMS/GetdataTemGoi`,
        type: "GET",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        traditional: true,
        data: params,
        success: function (data, st) {
           
            if (st == "success") {
                //if (data.records == 0) {
                //    ErrorAlert("Không có mã tem gói có sẵn.")
                //}
                var showing = grid[0];
                showing.addJSONData(data);
            }
        },
        error: function () {
            return;
        }
    });
};

$("#clearQRBtn").on("click", function () {
    var r = confirm("Bạn có chắc muốn xóa hết dữ liệu trên bảng này không?");
    if (r === true) {
        grid.jqGrid('clearGridData');
    }

});
insertmaterial_tmp = [];
json_object = "";
$(document).ready(function () {
    function changeDate(exdate) {
        var e0date = new Date(0);
        var offset = e0date.getTimezoneOffset();
        return new Date(0, 0, exdate - 1, 0, -offset, 0);
    };
    $("#imgupload").change(function (evt) {
        insertmaterial_tmp = [];
        json_object = "";
        var selectedFile = evt.target.files[0];
        var reader = new FileReader();
        var excelData = [];
        reader.onload = function (event) {
            var data = event.target.result;
            var workbook = XLSX.read(data, { type: 'binary' });
            workbook.SheetNames.forEach(function (sheetName) {
                var XL_row_object = XLSX.utils.sheet_to_row_object_array(workbook.Sheets[sheetName]);
                json_object = XL_row_object;
            })
        };

        reader.onerror = function (event) {
            console.error("File could not be read! Code " + event.target.error.code);
        };
        reader.readAsBinaryString(selectedFile);
    });
});
$("#uploadBtn").click(function () {
    if ($("#imgupload").val() == "") { return false; }
    $('#loading').show();
    var data_create = 0;
    var data_update = 0;
    var data_error = 0;
    var data_pagedata = -1;
   
    for (var i = 0; i < json_object.length; i++) {
        var mt_no = json_object[i]["CODE NVL"];
        if (mt_no != null) {
            item = {
                md_cd: json_object[i]["MODEL"],
                style_no: json_object[i]["CODE PRODUCT"],
                mt_no: json_object[i]["CODE NVL"],
                cav: json_object[i]["CAVIT"],
                need_time: json_object[i]["Số lần sử dụng"],
                buocdap: json_object[i]["Bước dập/Số đo (mm)"],
            }
            insertmaterial_tmp.push(item);
        }

    }
        $.ajax({
            url: "/DevManagement/Upload_BOM",
            type: 'POST',
            async: true,
            dataType: 'json',
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(insertmaterial_tmp),
            traditonal: true,
            success: function (data) {
                if (data.result) {
                    SuccessAlert(data.message);
                    for (var i = 0; i < data.danhsach.length; i++) {
                        var id = data.danhsach[i].bid;
                        $("#list").jqGrid('addRowData', id, data.danhsach[i], 'first');
                        $("#list").setRowData(id, false, { background: "#d0e9c6" });
                    }
                }
                else {
                    var html = "";
                    if (data.style != 0) {
                        html+="Product đã tồn tại!"
                    }
                    if (data.mt_cd != 0) {
                        html += "Material đã tồn tại!"
                    }
                    if (data.exit_style != 0) {
                        html += "Product không tồn tại!"
                    }
                    if (data.cav != 0) {
                        html += "Cavit trống!"
                    }
                    if (data.buocdap != 0) {
                        html += "Bước Dập trống!"
                    }
                    if (data.need_time != 0) {
                        html += "Số lần sử dụng trống!"
                    }
                    ErrorAlert("Vì một số nguyên nhân sau:" + html);
                }
            },
        });
    $("#imgupload").val("");
    $('#loading').hide();
});

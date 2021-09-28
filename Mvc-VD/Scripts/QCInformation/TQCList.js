function table_facline_qc_value() {
    $("#list_facline_qc_value").jqGrid
        ({
            //url: "/TIMS/Getfacline_qc_value",
            datatype: 'json',
            mtype: 'Get',
            colModel: [
                { key: true, label: 'fqhno', name: 'fqhno', width: 80, align: 'center', hidden: true },
                { key: false, label: 'Subject', name: 'check_subject', sortable: true, width: '150', align: 'left', hidden: true },
                { key: false, label: 'Content', name: 'check_value', sortable: true, width: '150', align: 'left' },
                {
                    key: false, label: 'Defect Qty', name: 'check_qty', sortable: true, align: 'right', width: '90', editable: false,
                    editoptions: { size: 10, maxlength: 15 }, editrules: { integer: true, required: true },
                    formatter: 'integer', editoptions: {
                        dataInit: function (element) {
                            $(element).keypress(function (e) {
                                if (e.which != 8 && e.which != 0 && (e.which < 48 || e.which > 57)) {
                                    return false;
                                }
                            });
                        }
                    },
                },
                { key: false, label: 'Date', name: 'date_ymd', sortable: true, width: '90', align: 'center', hidden: true },
            ],

            onCellSelect: editRow_facline,
            onSelectRow: function (rowid, selected, status, e) {
                $("#p_SaveQcfacline").attr("disabled", false);
            },
            footerrow: true,
            userDataOnFooter: true,
            pager: "#list_gridpager_value",
            viewrecords: true,
            rowList: [50, 100, 200, 500,1000],
            height: 300,
            autowidth: true,
            width: null,
            rowNum: 50,
            caption: 'TQC Value',
            loadtext: "Loading...",
            emptyrecords: "No data.",
            rownumbers: true,
            gridview: true,
            shrinkToFit: false,
            loadonce: false,
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
            gridComplete: function () {
                prevCellVal = { cellId: undefined, value: undefined };

                var ids = $("#list_facline_qc_value").jqGrid('getDataIDs');
                var sum_Quantity_total = 0;
                var $self = $(this)
                for (var i = 0; i < ids.length; i++) {

                    sum_Quantity_total += parseInt($("#list_facline_qc_value").getCell(ids[i], "check_qty"));
                   
                }
                $self.jqGrid("footerData", "set", { check_value: "Total" });
                $self.jqGrid("footerData", "set", { check_qty: sum_Quantity_total });
            },
        })
}
table_facline_qc_value();
//delete Qc value 
function FormatDelQc_value(cellValue, options, rowdata, action) {
    var id = rowdata.wmtid;
    html = '<button  class="btn btn-sm btn-danger button-srh"  data-id="' + rowdata.fqhno + '"onclick="Click_DelQC_value(this);">X</button>';
    return html;
}
var lastSel;
function editRow_facline(id) {
    if (id && id !== lastSel) {
        var grid = $("#list_facline_qc_value");
        grid.jqGrid('editRow', id, { keys: true, focusField: 2 });
        lastSel = id;
    }
}
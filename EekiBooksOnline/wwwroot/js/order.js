let table;


$(document).ready(
    function () {
        var url = window.location.search;

        if (url.includes("inprocess")) {
            loadDataTable("inprocess");
        } else {
            if (url.includes("completed")) {
                loadDataTable("completed");
            } else {
                if (url.includes("pending")) {
                    loadDataTable("pending");
                } else {
                    if (url.includes("approved")) {
                        loadDataTable("approved");
                    } else {
                        loadDataTable("all");
                    }
                }
            }
        }
    }
)


function loadDataTable(status) {


    table = $('#tbldata').DataTable({
        "ajax": {
            "url": "/Admin/Order/GetAll?status=" + status
        },
        "columns": [
            { "data": "id", "width": "5%" },
            { "data": "name", "width": "20%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "10%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                         <div class="w-auto btn-group" role="group">
                        <a href="/Admin/Order/Detail?orderId=${data}" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                    </div>
                    `
                },
                "width": "10%"
            },
        ]
    });
}




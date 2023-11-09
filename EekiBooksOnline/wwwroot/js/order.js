let table;

$(document).ready(function () {
    table = $('#tbldata').DataTable({
        "ajax": {
            "url": "/Admin/Order/GetAll",
        },
        "columns": [
            { "data": "id", "width": "15%" },
            { "data": "name", "width": "15%" },
            { "data": "phoneNumber", "width": "15%" },
            { "data": "applicationUser.email", "width": "15%" },
            { "data": "orderStatus", "width": "15%" },
            { "data": "orderTotal", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                         <div class="w-auto btn-group" role="group">
                        <a href="/Admin/Order/Details?orderid=${data}" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                    </div>
                    `
                },
                "width": "15%"
            },
        ]
    });
});


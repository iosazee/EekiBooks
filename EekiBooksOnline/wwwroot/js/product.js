let table;

$(document).ready(function () {
    table = $('#tbldata').DataTable({
        "ajax": {
            "url": "/Admin/Product/GetAll",
        },
        "columns": [
            { "data": "title", "width": "15%" },
            { "data": "isbn", "width": "15%" },
            { "data": "price", "width": "15%" },
            { "data": "author", "width": "15%" },
            { "data": "category.name", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                         <div class="w-auto btn-group" role="group">
                        <a href="/Admin/Product/Upsert?id=${data}" class="btn btn-primary mx-2">
                            <i class="bi bi-pencil-square"></i>
                        </a>
                    </div>
                    `
                },
                "width": "15%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                     <div class="w-auto btn-group" role="group">
                        <a onClick=Delete('/Admin/Product/Delete?id=${data}')
                            class="btn btn-danger mx-2">
                            <i class="bi bi-trash3"></i>
                        </a>
                    </div>
                    `
                },
                "width": "15%"
            },
        ]
    });
});

function Delete(url) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    if (data.success) {
                        table.ajax.reload();
                        toastr.success(data.message)
                    }
                    else {
                        toastr.error(data.message)
                    }
                }
            })
        }
    })
}
// Hiển thị ảnh được chọn từ input file lên thẻ img
// (Thẻ input có thuộc tính data-img-preview trỏ đến id của thẻ img dung để hiển thị ảnh)
function previewImage(input) {
    if (!input.files || !input.files[0]) return;

    const previewId = input.dataset.imgPreview; // lấy data-img-preview
    if (!previewId) return;

    const img = document.getElementById(previewId);
    if (!img) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

// Tìm kiếm phân trang bằng AJAX
function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url = form.action;
    const method = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;

    const formData = new FormData(form);
    formData.append("page", page);

    let fetchUrl = url;
    if (method === "GET") {
        const params = new URLSearchParams(formData).toString();
        fetchUrl = url + "?" + params;
    }

    let targetEl = null;
    if (targetId) {
        targetEl = document.getElementById(targetId);
        if (targetEl) {
            targetEl.innerHTML = `
                <div class="text-center py-4">
                    <span>Đang tải dữ liệu...</span>
                </div>`;
        }
    }

    fetch(fetchUrl, {
        method: method,
        body: method === "GET" ? null : formData
    })
    .then(res => res.text())
    .then(html => {
        if (targetEl) {
            targetEl.innerHTML = html;
        }
    })
    .catch(() => {
        if (targetEl) {
            targetEl.innerHTML = `
                <div class="text-danger">
                    Không tải được dữ liệu
                </div>`;
        }
    });
}



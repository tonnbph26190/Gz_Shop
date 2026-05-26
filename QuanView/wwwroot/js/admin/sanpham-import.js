// Import Modal Handler for Product Management
(function () {
    'use strict';

    let currentStep = 1;
    let csvContent = '';

    const modal = document.getElementById('importModal');
    const step1 = document.getElementById('importStep1');
    const step2 = document.getElementById('importStep2');
    const step3 = document.getElementById('importStep3');
    const stepTitle = document.getElementById('modalStepTitle');

    const csvFileInput = document.getElementById('importCsvFile');
    const csvContentTextarea = document.getElementById('importCsvContent');
    const btnNextStep1 = document.getElementById('btnNextStep1');
    const btnBackStep2 = document.getElementById('btnBackStep2');
    const btnConfirmImport = document.getElementById('btnConfirmImport');
    const btnImportMore = document.getElementById('btnImportMore');

    function getRequestVerificationToken() {
        const tokenInput =
            document.querySelector('#importAntiForgeryToken input[name="__RequestVerificationToken"]') ||
            document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    // CSV Parser
    function parseCsvLine(line) {
        const result = [];
        let current = '';
        let inQuotes = false;
        
        for (let i = 0; i < line.length; i++) {
            const c = line[i];
            if (c === '"') {
                if (inQuotes && i + 1 < line.length && line[i + 1] === '"') {
                    current += '"';
                    i++;
                } else {
                    inQuotes = !inQuotes;
                }
            } else if ((c === ',' && !inQuotes) || c === '\n' || c === '\r') {
                result.push(current);
                current = '';
                if (c === '\n' || c === '\r') break;
            } else {
                current += c;
            }
        }
        result.push(current);
        return result;
    }

    // File upload handler
    csvFileInput.addEventListener('change', function (e) {
        const file = e.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = function (event) {
            csvContentTextarea.value = event.target.result;
        };
        reader.readAsText(file, 'UTF-8');
    });

    // Step 1: Validate and move to step 2
    btnNextStep1.addEventListener('click', async function () {
        csvContent = csvContentTextarea.value.trim();
        
        if (!csvContent) {
            alert('Vui lòng nhập hoặc tải lên dữ liệu CSV');
            return;
        }

        // Show loading
        btnNextStep1.disabled = true;
        btnNextStep1.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang xử lý...';

        try {
            // Validate data via AJAX
            const response = await fetch('/Admin/SanPham/ValidateImportData', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ csvContent: csvContent })
            });

            const result = await response.json();

            if (!result.success) {
                alert('Lỗi validate: ' + result.message);
                return;
            }

            // Show preview
            showPreview(csvContent, result);
            goToStep(2);

        } catch (error) {
            alert('Lỗi kết nối: ' + error.message);
        } finally {
            btnNextStep1.disabled = false;
            btnNextStep1.innerHTML = 'Tiếp tục <i class="fas fa-arrow-right"></i>';
        }
    });

    // Show preview table
    function showPreview(csv, validationResult) {
        const lines = csv.split(/\r\n|\r|\n/).filter(l => l.trim().length > 0);
        const previewTable = document.getElementById('previewTable');
        const thead = previewTable.querySelector('thead');
        const tbody = previewTable.querySelector('tbody');
        
        thead.innerHTML = '';
        tbody.innerHTML = '';

        if (lines.length === 0) return;

        // Header
        const headerCells = parseCsvLine(lines[0]);
        let headerRow = '<tr>';
        headerCells.forEach(cell => {
            headerRow += `<th>${escapeHtml(cell)}</th>`;
        });
        headerRow += '</tr>';
        thead.innerHTML = headerRow;

        // Data rows (limit to first 50 for preview)
        const maxRows = Math.min(lines.length, 51);
        for (let i = 1; i < maxRows; i++) {
            const cells = parseCsvLine(lines[i]);
            let row = '<tr>';
            headerCells.forEach((_, idx) => {
                const val = cells[idx] !== undefined ? cells[idx] : '';
                row += `<td>${escapeHtml(val)}</td>`;
            });
            row += '</tr>';
            tbody.innerHTML += row;
        }

        // Show validation summary
        const validationSuccess = document.getElementById('validationSuccess');
        const validationSummary = document.getElementById('validationSummary');
        const validationWarnings = document.getElementById('validationWarnings');
        const warningList = document.getElementById('warningList');

        validationSummary.textContent = `Tìm thấy ${validationResult.validRows} dòng hợp lệ, ${validationResult.errorRows} dòng lỗi`;
        validationSuccess.style.display = 'block';

        if (validationResult.warnings && validationResult.warnings.length > 0) {
            warningList.innerHTML = '';
            validationResult.warnings.forEach(warning => {
                const li = document.createElement('li');
                li.textContent = warning;
                warningList.appendChild(li);
            });
            
            if (validationResult.totalWarnings > validationResult.warnings.length) {
                const li = document.createElement('li');
                li.innerHTML = `<em>... và ${validationResult.totalWarnings - validationResult.warnings.length} cảnh báo khác</em>`;
                warningList.appendChild(li);
            }
            
            validationWarnings.style.display = 'block';
        } else {
            validationWarnings.style.display = 'none';
        }
    }

    // Step 2: Back to step 1
    btnBackStep2.addEventListener('click', function () {
        goToStep(1);
    });

    // Step 2: Confirm and import
    btnConfirmImport.addEventListener('click', async function () {
        btnConfirmImport.disabled = true;
        btnConfirmImport.innerHTML = '<span class="spinner-border spinner-border-sm"></span> Đang import...';
        
        goToStep(3);
        
        try {
            // Submit to actual import endpoint
            const formData = new FormData();
            formData.append('csvContent', csvContent);
            const requestToken = getRequestVerificationToken();
            if (requestToken) {
                formData.append('__RequestVerificationToken', requestToken);
            }

            const response = await fetch('/Admin/SanPham/ImportCsv', {
                method: 'POST',
                body: formData
            });

            const html = await response.text();
            
            // Parse the result from the returned HTML
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, 'text/html');
            
            // Extract success and error rows from ImportCsv.cshtml result tables
            const successRows = doc.querySelectorAll('.card.border-success tbody tr');
            const errorRows = doc.querySelectorAll('.card.border-danger tbody tr');
            
            showImportResults(successRows, errorRows);

        } catch (error) {
            alert('Lỗi import: ' + error.message);
            goToStep(2);
        } finally {
            btnConfirmImport.disabled = false;
            btnConfirmImport.innerHTML = '<i class="fas fa-check"></i> Xác nhận Import';
        }
    });

    // Show import results
    function showImportResults(successRows, errorRows) {
        document.getElementById('importProgress').style.display = 'none';
        document.getElementById('importResults').style.display = 'block';
        document.getElementById('resultActions').style.display = 'flex';

        const successSection = document.getElementById('successSection');
        const successCount = document.getElementById('successCount');
        const successList = document.getElementById('successList');
        const errorSection = document.getElementById('errorSection');
        const errorCount = document.getElementById('errorCount');
        const errorList = document.getElementById('errorList');

        if (successRows.length > 0) {
            successCount.textContent = successRows.length;
            successList.innerHTML = '';
            successRows.forEach(row => {
                successList.appendChild(row.cloneNode(true));
            });
            successSection.style.display = 'block';
        }

        if (errorRows.length > 0) {
            errorCount.textContent = errorRows.length;
            errorList.innerHTML = '';
            errorRows.forEach(row => {
                errorList.appendChild(row.cloneNode(true));
            });
            errorSection.style.display = 'block';
        }

        // If all successful, show success message
        if (successRows.length > 0 && errorRows.length === 0) {
            setTimeout(() => {
                if (confirm('Import thành công! Bạn có muốn tải lại trang để xem kết quả?')) {
                    window.location.reload();
                }
            }, 500);
        }
    }

    // Import more button
    btnImportMore.addEventListener('click', function () {
        resetModal();
        goToStep(1);
    });

    // Step navigation
    function goToStep(step) {
        currentStep = step;
        
        step1.classList.add('d-none');
        step2.classList.add('d-none');
        step3.classList.add('d-none');

        if (step === 1) {
            step1.classList.remove('d-none');
            stepTitle.textContent = 'Bước 1/3 - Tải dữ liệu';
        } else if (step === 2) {
            step2.classList.remove('d-none');
            stepTitle.textContent = 'Bước 2/3 - Xem trước dữ liệu';
        } else if (step === 3) {
            step3.classList.remove('d-none');
            stepTitle.textContent = 'Bước 3/3 - Kết quả';
            document.getElementById('importProgress').style.display = 'block';
            document.getElementById('importResults').style.display = 'none';
        }
    }

    // Reset modal
    function resetModal() {
        csvContent = '';
        csvContentTextarea.value = '';
        csvFileInput.value = '';
        currentStep = 1;
        
        document.getElementById('validationSuccess').style.display = 'none';
        document.getElementById('validationWarnings').style.display = 'none';
        document.getElementById('successSection').style.display = 'none';
        document.getElementById('errorSection').style.display = 'none';
        document.getElementById('resultActions').style.display = 'none';
    }

    // Reset on modal close
    if (modal) {
        modal.addEventListener('hidden.bs.modal', function () {
            resetModal();
            goToStep(1);
        });
    }

    // Utility function
    function escapeHtml(text) {
        const map = {
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#039;'
        };
        return text.replace(/[&<>"']/g, m => map[m]);
    }

})();

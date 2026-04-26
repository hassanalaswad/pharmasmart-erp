// حالة التطبيق (State Management)
window.appState = {
    model: 'prophet', // 'prophet' | 'vertex'
    horizon: 3,       // 1, 3, 6, 12 months
    safetySwitch: false,
    forecastData: [], // سيتم تغذيته من Razor ViewBag
    chartLabels: [],
    chartActual: [],
    chartForecast: [],
    totalExpectedCost: 0
};

document.addEventListener('DOMContentLoaded', function() {
    initUI();
    initChart();
    renderDraftGrid();
    updateStatusBar(1); // المرحلة الأولى
});

function initUI() {
    // 1. Model Selection (Prophet vs Vertex)
    const localBtn = document.getElementById('localBtn');
    const cloudBtn = document.getElementById('cloudBtn');
    const cloudArea = document.getElementById('cloudUploadArea');

    if (localBtn && cloudBtn) {
        localBtn.addEventListener('click', () => {
            appState.model = 'prophet';
            localBtn.className = 'px-4 py-1.5 rounded-full text-sm font-bold bg-primary text-white shadow-sm transition';
            cloudBtn.className = 'px-4 py-1.5 rounded-full text-sm font-bold bg-white border border-slate-300 text-slate-700 shadow-sm transition hover:bg-slate-50';
            if (cloudArea) cloudArea.classList.add('hidden');
            recalculateForecast();
        });
        cloudBtn.addEventListener('click', () => {
            appState.model = 'vertex';
            cloudBtn.className = 'px-4 py-1.5 rounded-full text-sm font-bold bg-primary text-white shadow-sm transition';
            localBtn.className = 'px-4 py-1.5 rounded-full text-sm font-bold bg-white border border-slate-300 text-slate-700 shadow-sm transition hover:bg-slate-50';
            if (cloudArea) cloudArea.classList.remove('hidden');
            recalculateForecast();
        });
    }

    // 1b. Data Source Sync Button
    const refreshDbBtn = document.getElementById('refreshDbBtn');
    if (refreshDbBtn) {
        refreshDbBtn.addEventListener('click', () => {
            window.location.reload();
        });
    }

    // 1c. Recalculate Dynamic Safety Stock
    const recalcSafetyStockBtn = document.getElementById('recalcSafetyStockBtn');
    if (recalcSafetyStockBtn) {
        recalcSafetyStockBtn.addEventListener('click', async () => {
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
            
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'تحديث حد الأمان...',
                    text: 'يتم الآن تحليل سرعة البيع لآخر 30 يوم وتحديث مخزون الأمان لكل صنف.',
                    allowOutsideClick: false,
                    didOpen: () => { Swal.showLoading(); }
                });
            }

            try {
                const response = await fetch('/InventoryIntelligence/RecalculateSafetyStock', {
                    method: 'POST',
                    headers: { 'RequestVerificationToken': token }
                });

                const data = await response.json();
                if (data.success) {
                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'success',
                            title: 'تم التحديث!',
                            text: data.message,
                            confirmButtonText: 'حسناً'
                        }).then(() => window.location.reload());
                    } else {
                        alert(data.message);
                        window.location.reload();
                    }
                } else {
                    if (typeof Swal !== 'undefined') Swal.fire('خطأ', data.message, 'error');
                }
            } catch (err) {
                console.error(err);
                if (typeof Swal !== 'undefined') Swal.fire('خطأ', 'فشل الاتصال بالمخدم لتحديث حد الأمان', 'error');
            }
        });
    }

    // 1c. PDF Button
    const customPdfBtn = document.getElementById('customPdfBtn');
    if (customPdfBtn) {
        customPdfBtn.addEventListener('click', () => {
            const modal = document.getElementById('printConfigModal');
            if (modal) {
                modal.classList.remove('hidden');
                setTimeout(() => modal.classList.remove('opacity-0'), 10);
            }
        });
    }

    // 2. Horizon Slider
    const horizonSlider = document.getElementById('horizonSlider');
    const horizonLabels = document.querySelectorAll('.horizon-labels span');
    if (horizonSlider) {
        horizonSlider.addEventListener('input', (e) => {
            const val = parseInt(e.target.value);
            appState.horizon = val;
            
            // Highlight active label
            horizonLabels.forEach(lbl => {
                if(parseInt(lbl.dataset.val) === val) {
                    lbl.classList.add('text-primary', 'font-black');
                    lbl.classList.remove('text-slate-500', 'font-medium');
                } else {
                    lbl.classList.remove('text-primary', 'font-black');
                    lbl.classList.add('text-slate-500', 'font-medium');
                }
            });
            recalculateForecast();
        });
    }

    // 3. Safety Switch (Vital Drugs)
    const safetySwitch = document.getElementById('safetySwitch');
    if (safetySwitch) {
        safetySwitch.addEventListener('change', (e) => {
            appState.safetySwitch = e.target.checked;
            renderDraftGrid();
        });
    }

    // 4. Approval Button
    const approvePlanBtn = document.getElementById('approvePlanBtn');
    if (approvePlanBtn) {
        approvePlanBtn.addEventListener('click', () => {
            if (typeof Swal !== 'undefined') {
                Swal.fire({
                    title: 'اعتماد الخطة؟',
                    text: 'سيتم إنشاء مسودات طلبات شراء (PO) لهذه الأصناف',
                    icon: 'question',
                    showCancelButton: true,
                    confirmButtonText: 'نعم، اعتماد الآن',
                    cancelButtonText: 'إلغاء',
                    confirmButtonColor: '#059669' // Emerald-600
                }).then((result) => {
                    if (result.isConfirmed) {
                        approvePlanBtn.innerHTML = '<span class="material-symbols-outlined text-[16px]">check_circle</span> تم الاعتماد ✓';
                        approvePlanBtn.classList.replace('bg-emerald-600', 'bg-emerald-800');
                        updateStatusBar(2);
                        showPOCards();
                    }
                });
            } else {
                approvePlanBtn.innerHTML = '<span class="material-symbols-outlined text-[16px]">check_circle</span> تم الاعتماد ✓';
                approvePlanBtn.classList.replace('bg-emerald-600', 'bg-emerald-800');
                updateStatusBar(2);
                showPOCards();
            }
        });
    }

    // 5. AI Magic Panel Open
    const openMagicPanel = document.getElementById('openMagicPanel');
    const aiMagicDrawer = document.getElementById('aiMagicDrawer');
    const closeMagicPanel = document.getElementById('closeMagicPanel');
    if (openMagicPanel && aiMagicDrawer) {
        openMagicPanel.addEventListener('click', () => {
            aiMagicDrawer.classList.remove('hidden');
            // Trigger step animation
            animateAISteps();
        });
        closeMagicPanel.addEventListener('click', () => {
            aiMagicDrawer.classList.add('hidden');
        });
    }

    // 6. External File Input Handler
    const externalFileInput = document.getElementById('externalFileInput');
    const uploadAreaText = document.getElementById('uploadAreaText');
    if(externalFileInput && uploadAreaText) {
        externalFileInput.addEventListener('change', (e) => {
            if(e.target.files && e.target.files.length > 0) {
                const file = e.target.files[0];
                uploadAreaText.innerText = file.name;
                
                // Read via SheetJS
                const reader = new FileReader();
                reader.onload = function(evt) {
                    try {
                        const data = evt.target.result;
                        const workbook = XLSX.read(data, { type: 'binary' });
                        const firstSheetName = workbook.SheetNames[0];
                        const worksheet = workbook.Sheets[firstSheetName];
                        
                        // Extract JSON and headers
                        const json = XLSX.utils.sheet_to_json(worksheet, { header: 1 });
                        if(json.length > 0) {
                            appState.tempExcelData = XLSX.utils.sheet_to_json(worksheet);
                            const headers = json[0];
                            if (typeof window.openMappingModal === 'function') {
                                window.openMappingModal(headers);
                            }
                        } else {
                            if(typeof Swal !== 'undefined') Swal.fire('خطأ', 'الملف فارغ', 'error');
                        }
                    } catch (err) {
                        console.error(err);
                        if(typeof Swal !== 'undefined') Swal.fire('خطأ', 'حدث خطأ أثناء قراءة الملف', 'error');
                    }
                };
                reader.readAsBinaryString(file);
            } else {
                uploadAreaText.innerText = 'اسحب وافلت أو انقر لاختيار ملف (CSV / Excel)';
            }
        });
    }
}

// Global Chart Instance
let forecastChartInstance = null;

function initChart() {
    const chartCtx = document.getElementById('forecastChart');
    if (!chartCtx) return;

    if (forecastChartInstance) forecastChartInstance.destroy();

    // Adjust forecast array length based on horizon
    let projectedForecast = [...appState.chartForecast];
    if (appState.horizon > Object.keys(appState.chartForecast).length) {
        // Just pad for visualization
        projectedForecast = [...appState.chartForecast, ...Array(appState.horizon).fill(appState.chartForecast[appState.chartForecast.length-1]*1.02)];
    }

    forecastChartInstance = new Chart(chartCtx.getContext('2d'), {
        type: 'line',
        data: {
            labels: appState.chartLabels,
            datasets: [
                { 
                    label: 'المبيعات الفعلية', 
                    data: appState.chartActual, 
                    borderColor: '#3b82f6', 
                    backgroundColor: 'rgba(59,130,246,0.08)', 
                    tension: 0.3, 
                    fill: true, 
                    pointBackgroundColor: '#3b82f6', 
                    borderWidth: 2.5,
                    borderJoinStyle: 'round'
                },
                { 
                    label: 'التوقعات (' + (appState.model === 'vertex' ? 'Vertex AI' : 'Prophet') + ')', 
                    data: projectedForecast, 
                    borderColor: '#f59e0b', 
                    backgroundColor: 'transparent', 
                    borderDash: [5, 5], 
                    tension: 0.3, 
                    fill: false, 
                    pointBackgroundColor: '#f59e0b', 
                    borderWidth: 2 
                }
            ]
        },
        options: { 
            responsive: true, 
            maintainAspectRatio: false,
            plugins: { 
                legend: { position: 'top', labels: { font: { family: 'Tajawal', weight: '700' }, boxWidth: 12 } },
                tooltip: {
                    callbacks: {
                        label: function(context) { return context.dataset.label + ': ' + context.parsed.y; }
                    }
                }
            },
            interaction: { mode: 'index', intersect: false },
            scales: { x: { grid: { display: false } }, y: { grid: { color: '#f1f5f9' }, beginAtZero: true } }
        }
    });
}

function renderDraftGrid() {
    const tbody = document.getElementById('forecastTableBody');
    if (!tbody) return;
    
    tbody.innerHTML = '';
    appState.totalExpectedCost = 0;

    // Get Total Liquidity from DOM safely
    const liquidityEl = document.getElementById('totalLiquidityDisplay');
    const totalLiquidity = liquidityEl ? parseFloat(liquidityEl.innerText.replace(/[^0-9.]/g, '')) || 0 : 0;

    let visibleCount = 0;
    appState.forecastData.forEach((item, idx) => {
        // Safety switch effect (vital drugs A items)
        // Note: isLifeSaving and priority come from the backend now
        const isVital = item.isLifeSaving || item.priority === 'قصوى';
        let finalApproved = item.approved;
        let isBoosted = false;

        if (appState.safetySwitch && isVital) {
            finalApproved = Math.round(item.optimalQty * 1.5);
            isBoosted = true;
        } else if (!appState.safetySwitch && isVital) {
            finalApproved = item.optimalQty;
        }

        // ── الكميات قادمة من الـ Backend بوحدة الشراء (MainUnit) مباشرةً ──
        const cf = (item.conversionFactor && item.conversionFactor > 1) ? item.conversionFactor : 1;
        // finalApproved بوحدة الشراء بالفعل — لا نقسم على cf مرة أخرى
        const displayQty  = Math.ceil(finalApproved);
        const displayCost = displayQty * parseFloat(item.unitCost || 0);
        const displayUnit = item.unit || 'وحدة';   // MainUnit = وحدة الشراء

        // ─── إخفاء الأصناف ذات المخزون الكافي (لا تحتاج طلب) ───────────
        if (displayQty === 0 && item.status !== 'ناقص - يحتاج طلب') {
            return; // تخطي هذا الصنف كليًا
        }

        // Determine Financial Match dynamically for each row
        const cumulativeCost = appState.totalExpectedCost + displayCost;
        let financialStatus = "";
        let statusBadgeClass = "";

        if (cumulativeCost <= totalLiquidity) {
            financialStatus = "في حدود الميزانية";
            statusBadgeClass = "bg-emerald-100 text-emerald-800 border-emerald-200 border";
        } else {
            financialStatus = "عجز مالي - مؤجل";
            statusBadgeClass = "bg-rose-100 text-rose-800 border-rose-200 border";
        }

        appState.totalExpectedCost += displayCost;
        visibleCount++;

        const tr = document.createElement('tr');
        tr.className = `border-t transition-colors hover:bg-slate-50 ${isBoosted ? 'bg-rose-50/30' : ''}`;
        
        // Priority Badges
        let priorityBadge = '';
        if(item.priority === 'قصوى') priorityBadge = '<span class="px-2 py-0.5 rounded text-[10px] font-black bg-rose-100 text-rose-700">أولوية قصوى</span>';
        else if(item.priority === 'متوسطة') priorityBadge = '<span class="px-2 py-0.5 rounded text-[10px] font-black bg-amber-100 text-amber-700">أولوية متوسطة</span>';
        else priorityBadge = '<span class="px-2 py-0.5 rounded text-[10px] font-black bg-slate-200 text-slate-700">عادية</span>';

        tr.innerHTML = `
            <td class="px-3 py-3 w-8"><input type="checkbox" checked class="rounded border-slate-300 text-primary focus:ring-primary h-4 w-4"></td>
            <td class="px-3 py-3 font-bold text-slate-800">
                <div class="flex items-center gap-2 flex-wrap">
                    ${priorityBadge}
                    <span>${item.drug}</span>
                    ${isBoosted ? '<span class="text-rose-500" title="تم المضاعفة لقاطع الأمان"><span class="material-symbols-outlined text-[14px]">health_and_safety</span></span>' : ''}
                </div>
                <div class="flex items-center gap-1.5 mt-1">
                    <span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-blue-50 text-blue-700 border border-blue-100">
                        <span class="material-symbols-outlined text-[10px] align-middle">package_2</span> شراء: ${displayUnit}
                    </span>
                    ${(item.subUnit && cf > 1) ? `<span class="text-[10px] font-bold px-1.5 py-0.5 rounded bg-slate-100 text-slate-500 border border-slate-200">${cf} ${item.subUnit}/${displayUnit}</span>` : ''}
                </div>
            </td>
            <td class="px-3 py-2 text-center text-slate-500 font-bold">${Math.ceil(item.stock / cf)} <span class="text-xs font-normal">/ ${Math.ceil(item.minStock / cf)}</span></td>
            <td class="px-3 py-2 text-center font-bold text-amber-600">${item.expectedQty} <span class="text-[9px] text-slate-400">/شهر</span></td>
            <td class="px-3 py-2 text-center text-slate-500 font-bold">${item.optimalQty} <span class="text-[9px]">${displayUnit}</span></td>
            <td class="px-3 py-2 text-center bg-emerald-50/50 relative">
                <input type="number" 
                       value="${displayQty}" 
                       min="0"
                       class="w-20 border ${isBoosted ? 'border-rose-400 focus:ring-rose-500' : 'border-emerald-300 focus:ring-primary'} focus:border-primary focus:ring-1 rounded-sm px-1 py-1 text-center font-black text-slate-800" 
                       data-idx="${idx}"
                       data-cf="${cf}"
                       oninput="window._updateItemDisplayQty && window._updateItemDisplayQty(${idx}, this.value, ${cf})">
                <div class="text-[10px] mt-1">
                    <span class="text-slate-400">${formatCurrency(displayCost)}</span>
                    <span class="text-blue-600 font-bold mr-1">${displayUnit}</span>
                </div>
            </td>
            <td class="px-3 py-2 text-center">
                <span class="px-2 py-1 rounded text-[10px] font-bold ${statusBadgeClass}">${financialStatus}</span>
            </td>
        `;
        tbody.appendChild(tr);
    });

    // عرض رسالة إذا كانت جميع الأصناف مخزونها كافٍ
    if (visibleCount === 0) {
        const emptyRow = document.createElement('tr');
        emptyRow.innerHTML = `<td colspan="7" class="text-center py-12 text-slate-500">
            <span class="material-symbols-outlined text-4xl text-emerald-500 block mb-2">inventory_2</span>
            <div class="font-bold text-emerald-700">جميع الأصناف مخزونها كافٍ — لا يوجد ما يحتاج طلباً الآن</div>
        </td>`;
        tbody.appendChild(emptyRow);
    }

    // Update Bottom Summary and Deficit warning
    const costDisplay = document.getElementById('totalExpectedCost');
    const costContainer = document.getElementById('costContainer');
    const deficitWarning = document.getElementById('deficitWarning');
    const deficitAmount = document.getElementById('deficitAmount');
    
    if (costDisplay) {
        costDisplay.innerHTML = formatCurrency(appState.totalExpectedCost);
        
        if (appState.totalExpectedCost > totalLiquidity) {
            costContainer.classList.replace('text-slate-800', 'text-rose-800');
            costDisplay.classList.replace('text-primary', 'text-rose-600');
            if(deficitWarning && deficitAmount) {
                deficitWarning.classList.replace('hidden', 'flex');
                deficitAmount.innerHTML = formatCurrency(appState.totalExpectedCost - totalLiquidity);
            }
        } else {
            costContainer.classList.replace('text-rose-800', 'text-slate-800');
            costDisplay.classList.replace('text-rose-600', 'text-primary');
            if(deficitWarning) {
                deficitWarning.classList.replace('flex', 'hidden');
            }
        }
        
        costDisplay.classList.add('animate-pulse');
        setTimeout(() => costDisplay.classList.remove('animate-pulse'), 500);
    }
}

// Called when user edits the approved column — value entered is in PACKETS (MainUnit)
// cf = conversionFactor (sub-units per packet)
window._updateItemDisplayQty = function(idx, val, cf) {
    const packetsEntered = parseInt(val) || 0;
    const cfNum = (cf && cf > 1) ? parseInt(cf) : 1;
    // Convert back to sub-unit internally (strips = packets × conversionFactor)
    appState.forecastData[idx].approved = packetsEntered * cfNum;
    // Debounced re-render to avoid losing focus mid-type
    clearTimeout(window._updateQtyTimer);
    window._updateQtyTimer = setTimeout(() => renderDraftGrid(), 600);
}

// Legacy alias kept for safety
window.updateItemQty = function(idx, val) {
    const numericVal = parseInt(val) || 0;
    appState.forecastData[idx].approved = numericVal;
    renderDraftGrid();
}

async function recalculateForecast() {
    if(typeof Swal !== 'undefined') {
        Swal.fire({
            title: 'جاري التنبؤ...',
            text: 'يتم الآن تحليل البيانات عبر محرك ' + (appState.model === 'vertex' ? 'Vertex AI' : 'Prophet'),
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }

    try {
        const payload = {
            items: appState.forecastData,
            model: appState.model,
            horizon: appState.horizon
        };

        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';

        const response = await fetch('/InventoryIntelligence/RunForecast', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify(payload)
        });

        if (response.ok) {
            const data = await response.json();
            appState.forecastData = data.items;
            
            // Only update chart if data is returned
            if(data.chartLabels && data.chartLabels.length > 0) {
                appState.chartLabels = data.chartLabels;
                appState.chartActual = data.chartActual;
                appState.chartForecast = data.chartForecast;
            }

            initChart();
            renderDraftGrid();
            
            if(typeof Swal !== 'undefined') Swal.close();
        } else {
            if(typeof Swal !== 'undefined') Swal.fire('خطأ', 'فشل تشغيل خوارزمية التنبؤ بالمخدم', 'error');
        }
    } catch (err) {
        console.error(err);
        if(typeof Swal !== 'undefined') Swal.fire('خطأ', 'فشل الاتصال بالمخدم', 'error');
    }
}

function updateStatusBar(step) {
    const steps = document.querySelectorAll('.stepper-item');
    steps.forEach((el, index) => {
        const stepNum = index + 1;
        const iconDiv = el.querySelector('.step-icon');
        const textDiv = el.querySelector('.step-text');
        
        if (stepNum < step) {
            // Completed
            iconDiv.className = 'step-icon w-8 h-8 rounded-full flex items-center justify-center bg-primary text-white font-bold shadow-sm';
            iconDiv.innerHTML = '<span class="material-symbols-outlined text-[16px]">check</span>';
            textDiv.className = 'step-text text-xs font-bold text-slate-800 mt-1';
        } else if (stepNum === step) {
            // Active
            iconDiv.className = 'step-icon w-8 h-8 rounded-full flex items-center justify-center bg-blue-600 text-white font-bold ring-4 ring-blue-100 relative';
            iconDiv.innerHTML = stepNum;
            // Add pulse
            iconDiv.innerHTML += '<span class="absolute inset-0 rounded-full border-2 border-blue-600 animate-ping opacity-75"></span>';
            textDiv.className = 'step-text text-xs font-bold text-blue-700 mt-1';
        } else {
            // Pending
            iconDiv.className = 'step-icon w-8 h-8 rounded-full flex items-center justify-center bg-slate-100 text-slate-400 font-bold border border-slate-200';
            iconDiv.innerHTML = stepNum;
            textDiv.className = 'step-text text-xs font-medium text-slate-400 mt-1';
        }
    });
}

function showPOCards() {
    const poSection = document.getElementById('poCardsSection');
    if(!poSection) return;
    
    const approvedItems = appState.forecastData.filter(i => i.approved > 0);
    if(approvedItems.length === 0) {
        if(typeof Swal !== 'undefined') Swal.fire('تنبيه', 'لا توجد أصناف معتمدة لإنشاء طلبات توريد', 'warning');
        return;
    }

    const totalPOCost = approvedItems.reduce((sum, item) => sum + (item.approved * item.unitCost), 0);
    
    // بناء بطاقة PO ديناميكية للمورد المعتمد حالياً (كمثال نجمعهم في PO واحد)
    poSection.innerHTML = `
    <h3 class="font-black text-slate-800 text-lg mb-4 flex items-center gap-2">
        <span class="material-symbols-outlined text-primary text-[22px]">local_shipping</span> طلبات التوريد قيد الانتظار (PO)
    </h3>
    <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <div class="bg-white border border-slate-200 rounded-lg shadow-sm p-4 relative overflow-hidden group">
            <div class="absolute top-0 right-0 w-1 bg-amber-400 h-full"></div>
            <div class="flex justify-between items-start mb-3">
                <div>
                    <h4 class="font-bold text-slate-800">مورد: المورد الرئيسي المعتمد</h4>
                    <p class="text-[11px] text-slate-500 mt-1">رقم الطلب: #PO-${new Date().getFullYear()}${(new Date().getMonth()+1).toString().padStart(2,'0')}-01</p>
                </div>
                <span class="bg-amber-100 text-amber-700 text-[10px] font-bold px-2 py-0.5 rounded">بانتظار المورد</span>
            </div>
            
            <div class="bg-slate-50 rounded p-2 mb-4 text-xs">
                <div class="flex justify-between mb-1"><span class="text-slate-500">عدد الأصناف:</span><span class="font-bold">${approvedItems.length} صنف</span></div>
                <div class="flex justify-between"><span class="text-slate-500">التكلفة التقديرية:</span><span class="font-bold">${formatCurrency(totalPOCost)}</span></div>
            </div>

            <div class="flex gap-2">
                <button onclick="sharePO()" class="flex-1 bg-white border border-slate-300 text-slate-700 hover:bg-slate-50 py-1.5 rounded text-xs font-bold flex items-center justify-center gap-1 border-b-2 border-slate-300 hover:border-slate-400">
                    <span class="material-symbols-outlined text-[14px] text-green-600">send</span> WhatsApp
                </button>
                <button onclick="window.openReceiptModal()" class="flex-1 bg-blue-600 hover:bg-blue-700 text-white py-1.5 rounded text-xs font-bold shadow-sm transition flex items-center justify-center gap-1 border-b-2 border-blue-800">
                    <span class="material-symbols-outlined text-[14px]">inventory_2</span> استلام البضاعة
                </button>
            </div>
        </div>
    </div>`;

    poSection.classList.remove('hidden');
    poSection.scrollIntoView({ behavior: 'smooth' });
}

window.openReceiptModal = function() {
    const m = document.getElementById('receiptModal');
    const tbody = document.getElementById('receiptModalBody');
    if(tbody) {
        tbody.innerHTML = '';
        const approvedItems = appState.forecastData.filter(i => i.approved > 0);
        window._currentPlanId = window._currentPlanId || 0; // سيتم تعيينه عند توليد الخطة
        approvedItems.forEach((item, idx) => {
            const isVital = item.abc === 'A';
            const cf = (item.conversionFactor && item.conversionFactor > 1) ? item.conversionFactor : 1;
            const displayQty = Math.ceil(item.approved);
            const tr = document.createElement('tr');
            tr.className = `border-b focus-within:bg-blue-50/30 ${isVital ? 'bg-rose-50/30 border-rose-200' : ''}`;
            tr.dataset.drugId = item.drugId || 0;
            tr.innerHTML = `
                 <td class="p-2 font-bold flex items-center gap-1">
                     ${item.drug} 
                     ${isVital ? '<span class="text-rose-500 font-bold" title="دواء حيوي">♥</span>' : ''}
                     <span class="text-[10px] text-slate-400 font-normal">${item.unit || ''}</span>
                 </td>
                 <td class="p-2 text-center"><input type="number" value="${displayQty}" min="1" class="w-16 border border-slate-300 rounded px-1 py-1 text-center font-bold ${isVital ? 'text-rose-600' : ''}"></td>
                 <td class="p-2 text-center"><input type="number" value="${item.unitCost}" step="0.01" class="w-full border border-slate-300 rounded px-1 py-1 text-center font-bold focus:border-primary focus:ring-1 text-left" dir="ltr"></td>
                 <td class="p-2 text-center"><input type="text" placeholder="B-10${idx}" class="w-full border border-slate-300 rounded px-1 py-1 text-center focus:border-primary focus:ring-1 uppercase"></td>
                 <td class="p-2 text-center"><input type="month" class="w-full border border-slate-300 rounded px-1 py-1 text-center focus:border-primary focus:ring-1"></td>
            `;
            tbody.appendChild(tr);
        });
    }
    
    m.classList.remove('hidden');
    setTimeout(() => { m.classList.remove('opacity-0'); m.querySelector('.transform').classList.remove('scale-95'); }, 10);
};

window.closeReceiptModal = function() {
    const m = document.getElementById('receiptModal');
    if(m) {
        m.classList.add('opacity-0'); m.querySelector('.transform').classList.add('scale-95');
        setTimeout(() => { m.classList.add('hidden'); }, 300);
    }
};

window.confirmReceipt = async function() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const tbody = document.getElementById('receiptModalBody');
    if (!tbody) return;

    const rows = tbody.querySelectorAll('tr');
    const items = [];
    let hasError = false;

    rows.forEach((row, idx) => {
        const inputs = row.querySelectorAll('input');
        if (inputs.length < 4) return;

        const drugId   = parseInt(row.dataset.drugId || 0);
        const qty      = parseInt(inputs[0].value) || 0;
        const cost     = parseFloat(inputs[1].value) || 0;
        const batch    = inputs[2].value.trim();
        const expiry   = inputs[3].value; // yyyy-MM format from <input type="month">

        if (qty <= 0) { hasError = true; inputs[0].classList.add('border-red-500'); return; }
        items.push({
            DrugId: drugId,
            ReceivedQty: qty,
            UnitCost: cost,
            BatchNumber: batch || null,
            ExpiryDate: expiry ? expiry + '-01' : null
        });
    });

    if (hasError) {
        if (typeof Swal !== 'undefined') Swal.fire('تنبيه', 'يرجى إدخال كمية صحيحة لجميع الأصناف', 'warning');
        return;
    }
    if (items.length === 0) {
        if (typeof Swal !== 'undefined') Swal.fire('تنبيه', 'لا توجد أصناف للاستلام', 'warning');
        return;
    }

    window.closeReceiptModal();

    if (typeof Swal !== 'undefined') Swal.fire({ title: 'جاري الحفظ...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });

    try {
        const planId = window._currentPlanId || 0;
        const response = await fetch('/InventoryIntelligence/ReceiveGoods', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({ PlanId: planId, Items: items })
        });

        const data = await response.json();
        if (data.success) {
            if (typeof updateStatusBar === 'function') updateStatusBar(5);
            if (typeof Swal !== 'undefined') Swal.fire({ icon: 'success', title: 'تم الاستلام والترحيل ✅', text: data.message, confirmButtonColor: '#059669' });
            const poSection = document.getElementById('poCardsSection');
            if (poSection) poSection.innerHTML = '<div class="text-center p-8 text-emerald-600 font-bold"><span class="material-symbols-outlined text-4xl block mb-2">inventory</span> تم الاستلام وزيادة المخزون بنجاح.</div>';
        } else {
            if (typeof Swal !== 'undefined') Swal.fire('خطأ', data.message, 'error');
        }
    } catch(err) {
        console.error(err);
        if (typeof Swal !== 'undefined') Swal.fire('خطأ', 'فشل الاتصال بالمخدم أثناء الاستلام', 'error');
    }
};

function formatCurrency(val) {
    return val.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) + ' ' + (appState.currencySymbol || '');
}

function animateAISteps() {
    const steps = document.querySelectorAll('#aiMagicSteps li');
    steps.forEach(s => {
        s.classList.remove('opacity-100', 'translate-x-0');
        s.classList.add('opacity-0', '-translate-x-4');
        const icon = s.querySelector('.material-symbols-outlined');
        if(icon) {
            icon.innerText = 'hourglass_empty';
            icon.className = 'material-symbols-outlined text-amber-500 animate-spin text-[16px]';
        }
    });

    steps.forEach((s, i) => {
        setTimeout(() => {
            s.classList.remove('opacity-0', '-translate-x-4');
            s.classList.add('opacity-100', 'translate-x-0');
            
            // Wait a bit, then check mark
            setTimeout(() => {
                const icon = s.querySelector('.material-symbols-outlined');
                if(icon) {
                    icon.classList.remove('animate-spin', 'text-amber-500');
                    icon.classList.add('text-emerald-500');
                    icon.innerText = 'check_circle';
                }
            }, 600);

        }, i * 1000); // 1 sec per step
    });
}

// الدوال الخاصة بنافذة المطابقة (Mapping Modal)
window.openMappingModal = function(headers) {
    const selects = ['mapDrugName', 'mapExpectedQty'];
    selects.forEach(selId => {
        const el = document.getElementById(selId);
        if(el) {
            el.innerHTML = '<option value="">-- اختر العمود --</option>';
            headers.forEach(h => {
                if(h) el.innerHTML += `<option value="${h}">${h}</option>`;
            });
        }
    });
    
    document.getElementById('mappingError').classList.add('hidden');
    const m = document.getElementById('mappingModal');
    if(m) {
        m.classList.remove('hidden');
        setTimeout(() => { m.classList.remove('opacity-0'); m.querySelector('.transform').classList.remove('scale-95'); }, 10);
    }
};

window.closeMappingModal = function() {
    const m = document.getElementById('mappingModal');
    if(m) {
        m.classList.add('opacity-0'); m.querySelector('.transform').classList.add('scale-95');
        setTimeout(() => { m.classList.add('hidden'); }, 300);
    }
    document.getElementById('externalFileInput').value = '';
    document.getElementById('uploadAreaText').innerText = 'اسحب وافلت أو انقر لاختيار ملف (CSV / Excel)';
};

window.confirmMapping = function() {
    const colDrug = document.getElementById('mapDrugName').value;
    const colExpected = document.getElementById('mapExpectedQty').value;

    if(!colDrug || !colExpected) {
        document.getElementById('mappingErrorText').innerText = 'يرجى اختيار أعمدة لجميع الحقول الأساسية.';
        document.getElementById('mappingError').classList.remove('hidden');
        return;
    }
    
    // Check for unique mapping variables
    const selectedColumns = [colDrug, colExpected];
    const uniqueColumns = new Set(selectedColumns);
    if(uniqueColumns.size !== selectedColumns.length) {
        document.getElementById('mappingErrorText').innerText = 'لا يمكن اختيار نفس العمود لأكثر من حقل.';
        document.getElementById('mappingError').classList.remove('hidden');
        return;
    }

    if(appState.tempExcelData) {
        // بناء forecastData جديد من tempExcelData
        const mappedData = appState.tempExcelData.map((row, idx) => {
            let expected = parseFloat(row[colExpected]) || 0;
            
            return {
                id: idx + 1000, 
                drug: row[colDrug] || 'صنف غير معروف',
                stock: 0, // سيتم جلبه من السيرفر
                expectedQty: expected,
                minStock: 0, 
                unitCost: 0, // سيتم جلبه من السيرفر
                eoq: 0,
                proposed: 0,
                approved: 0,
                abc: expected > 500 ? 'A' : (expected > 100 ? 'B' : 'C'),
                status: "قيد التحليل...",
                statusClass: "ok"
            };
        });

        appState.forecastData = mappedData;
        window.closeMappingModal();
        if(typeof Swal !== 'undefined') Swal.fire('نجاح', 'تم استيراد ومطابقة البيانات بنجاح', 'success');
        
        // تشغيل التنبؤ الفعلي على السيرفر بالبيانات الجديدة
        recalculateForecast();
    }
};

// ==============================================
// 🖨️ نظام الطباعة الاحترافي المخصص لـ PDF
// ==============================================

window.closePrintModal = function() {
    const modal = document.getElementById('printConfigModal');
    if (modal) {
        modal.classList.add('opacity-0');
        setTimeout(() => modal.classList.add('hidden'), 300);
    }
};

window.generateCustomPdf = function() {
    closePrintModal();

    // استخراج الأعمدة المحددة
    const cols = {
        priority: document.getElementById('col_priority').checked,
        stock: document.getElementById('col_stock').checked,
        expected: document.getElementById('col_expected').checked,
        optimalQty: document.getElementById('col_optimal').checked,
        approved: document.getElementById('col_approved').checked,
        cost: document.getElementById('col_cost').checked,
        status: document.getElementById('col_status').checked
    };

    // إنشاء نافذة الطباعة
    const printWindow = window.open('', '_blank', 'width=1000,height=800');
    
    // بناء رأس المستند والـ CSS الاحترافي
    let html = `<!DOCTYPE html>
<html lang="ar" dir="rtl">
<head>
    <meta charset="UTF-8">
    <title>خطة المشتريات الذكية - PDF</title>
    <style>
        @import url('https://fonts.googleapis.com/css2?family=Cairo:wght@400;700;900&display=swap');
        body {
            font-family: 'Cairo', Arial, sans-serif;
            background: #fff;
            color: #1e293b;
            margin: 0;
            padding: 20px;
            font-size: 14px;
            direction: rtl;
        }
        @page { size: A4; margin: 1.5cm; }
        .header {
            text-align: center;
            border-bottom: 2px solid #0f172a;
            padding-bottom: 15px;
            margin-bottom: 20px;
        }
        .header h1 { margin: 0; font-size: 24px; color: #0f172a; font-weight: 900; }
        .header p { margin: 5px 0 0; color: #64748b; font-size: 12px; }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-bottom: 20px;
        }
        th, td {
            border: 1px solid #cbd5e1;
            padding: 10px;
            text-align: right;
            font-size: 12px;
        }
        th { background: #f8fafc; font-weight: 700; color: #334155; }
        .text-center { text-align: center; }
        .font-bold { font-weight: bold; }
        .approved-col { background: #ecfdf5 !important; color: #065f46; font-weight: 900; }
        .footer {
            margin-top: 30px;
            text-align: left;
            font-size: 12px;
            color: #64748b;
        }
    </style>
</head>
<body>
    <div class="header">
        <h1>مسودة خطة المشتريات الذكية</h1>
        <p>تاريخ استخراج الخطة: ${new Date().toLocaleString('ar-SA')}</p>
    </div>
    <table>
        <thead>
            <tr>
                <th>التسلسل</th>
                <th>اسم الدواء</th>`;

    // Headers
    if (cols.priority) html += `<th class="text-center">أولوية التوريد</th>`;
    if (cols.stock) html += `<th class="text-center">الرصيد / الحد</th>`;
    if (cols.expected) html += `<th class="text-center">الكمية المباعة سابقاً</th>`;
    if (cols.optimalQty) html += `<th class="text-center">الكمية المُثلى</th>`;
    if (cols.approved) html += `<th class="text-center approved-col">الكمية للتوريد</th>`;
    if (cols.approved) html += `<th class="text-center">وحدة الشراء</th>`;
    if (cols.cost) html += `<th class="text-center">التكلفة الإجمالية</th>`;
    if (cols.status) html += `<th class="text-center">المطابقة المالية</th>`;

    html += `</tr>
        </thead>
        <tbody>`;

    // Data Rows
    let totalCostAccumulator = 0;
    appState.forecastData.forEach((item, index) => {
        const approvedQty = item.approved || 0;
        const cost = approvedQty * parseFloat(item.unitCost || 0);
        totalCostAccumulator += cost;

        html += `<tr>
            <td class="text-center">${index + 1}</td>
            <td class="font-bold">${item.drug}</td>`;
            
        if (cols.priority) html += `<td class="text-center font-bold">${item.priority || '-'}</td>`;
        if (cols.stock) html += `<td class="text-center">${item.stock} / ${item.minStock}</td>`;
        if (cols.expected) html += `<td class="text-center">${item.expectedQty}</td>`;
        if (cols.optimalQty) html += `<td class="text-center">${item.optimalQty}</td>`;
        if (cols.approved) html += `<td class="text-center approved-col">${approvedQty}</td>`;
        if (cols.approved) html += `<td class="text-center font-bold text-blue-700">${item.unit || '-'}</td>`;
        if (cols.cost) html += `<td class="text-center">${cost.toLocaleString()} ${appState.currencySymbol || ''}</td>`;
        if (cols.status) html += `<td class="text-center">${item.status}</td>`;

        html += `</tr>`;
    });

    // Totals Row
    html += `</tbody>
        <tfoot>
            <tr>
                <td colspan="${2 + (cols.priority?1:0) + (cols.stock?1:0) + (cols.expected?1:0) + (cols.optimalQty?1:0)}" style="text-align: left; font-weight: bold; background: #f8fafc; border: none; border-bottom: 1px solid #cbd5e1; padding-left: 15px;">الإجمالي التقديري للتكلفة:</td>`;

    if (cols.approved) html += `<td class="text-center border-t border-slate-300 border-x"></td>`; // Empty under approved qty
    if (cols.approved) html += `<td class="text-center border-t border-slate-300 border-x"></td>`; // Empty under unit
    if (cols.cost) html += `<td class="text-center font-bold" style="background:#f1f5f9; color:#0f172a;">${totalCostAccumulator.toLocaleString()} ${appState.currencySymbol || ''}</td>`;
    
    // Fill remaining columns in footer to make it symmetric
    if (cols.status && !cols.cost) html += `<td></td>`;
    else if (cols.status) html += `<td></td>`;

    html += `</tr>
        </tfoot>
    </table>
    
    <div class="footer">
        <p>تم استخراج التقرير آلياً بواسطة نظام PharmaSmart ERP الذكي للاستخدام الداخلي والمراجعة المبدئية.</p>
    </div>
</body>
</html>`;

    printWindow.document.write(html);
    printWindow.document.close();
    
    // الانتظار قليلاً لتتحمل الخطوط، ثم استدعاء الطباعة التلقائية
    setTimeout(() => {
        printWindow.focus();
        printWindow.print();
        // Optional: you could printWindow.close() after printing, but users might want to save it. 
    }, 800);
};

window.savePlan = function() {
    if (typeof Swal !== 'undefined') {
        Swal.fire({
            icon: 'success',
            title: 'تم الحفظ',
            text: 'تم حفظ الخطة بنجاح، يمكنك الرجوع إليها لاحقاً.',
            confirmButtonColor: '#059669',
            timer: 2500
        });
    }
};

window.sharePlanViaWhatsApp = function() {
    // إغلاق النافذة المنبثقة للطباعة إذا كانت مفتوحة
    closePrintModal();
    
    // 1- توليد رسالة نصية للواتساب
    let message = "مرحباً،\nمرفق لكم مسودة خطة المشتريات الذكية المستخرجة من النظام:\n\n";
    message += `إجمالي الأصناف: ${appState.forecastData.filter(i => i.approved > 0).length}\n`;
    message += `إجمالي التكلفة: ${formatCurrency(appState.totalExpectedCost)}\n\n`;
    message += "يرجى الاطلاع على ملف الـ PDF المرفق.";
    
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
    
    // 2- استدعاء دالة الطباعة (لتحويلها إلى PDF) بحيث يتمكن المستخدم من حفظها
    // قبل إرسال الرسالة عبر واتساب
    setTimeout(() => {
        // نفتح نافذة الواتساب
        window.open(whatsappUrl, '_blank');
        // نولد الـ PDF
        setTimeout(() => {
            generateCustomPdf();
        }, 500);
    }, 100);
};

window.sharePO = function() {
    const approvedItems = appState.forecastData.filter(i => i.approved > 0);
    const totalPOCost = approvedItems.reduce((sum, item) => sum + (item.approved * item.unitCost), 0);
    
    let message = "عزيزي المورد،\nنرسل لكم طلب شراء جديد (PO) عبر نظام PharmaSmart:\n\n";
    message += `عدد الأصناف: ${approvedItems.length}\n`;
    message += `التكلفة التقديرية: ${formatCurrency(totalPOCost)}\n\n`;
    message += "يرجى تأكيد الطلب، تجدون التفاصيل في الـ PDF.";
    
    const whatsappUrl = `https://wa.me/?text=${encodeURIComponent(message)}`;
    
    window.open(whatsappUrl, '_blank');
    
    setTimeout(() => {
        generateCustomPdf();
    }, 500);
};

// Configuración
const API_BASE_URL = '/api';
const STORAGE_KEY_AUTH = 'seida_auth';
const STORAGE_KEY_ENVIOS = 'seida_envios';
const STORAGE_KEY_CONSULTAS = 'seida_consultas';

// Estado de la aplicación
let appState = {
    auth: {
        ruc: '',
        usuarioSOL: '',
        claveSol: '',
        codigoEnvio: ''
    },
    envios: [],
    consultas: [],
    selectedEnvios: new Set(),
    selectedConsultas: new Set(),
    isProcessing: false
};

// Elementos del DOM
const elements = {
    // Auth inputs
    ruc: document.getElementById('ruc'),
    usuarioSOL: document.getElementById('usuarioSOL'),
    claveSol: document.getElementById('claveSol'),
    codigoEnvio: document.getElementById('codigoEnvio'),
    ipAddress: document.getElementById('ipAddress'),
    macAddress: document.getElementById('macAddress'),
    btnRefresh: document.getElementById('btnRefresh'),

    // Tabs
    tabButtons: document.querySelectorAll('.tab-button'),
    tabContents: document.querySelectorAll('.tab-content'),

    // Envios
    numeroTransaccion: document.getElementById('numeroTransaccion'),
    archivoEnvio: document.getElementById('archivoEnvio'),
    btnAddEnvio: document.getElementById('btnAddEnvio'),
    btnRemoveEnvio: document.getElementById('btnRemoveEnvio'),
    enviosTableBody: document.getElementById('enviosTableBody'),
    selectAllEnvios: document.getElementById('selectAllEnvios'),
    btnEnviar: document.getElementById('btnEnviar'),

    // Consultas
    annoConsulta: document.getElementById('annoConsulta'),
    numeroTicket: document.getElementById('numeroTicket'),
    btnAddConsulta: document.getElementById('btnAddConsulta'),
    btnRemoveConsulta: document.getElementById('btnRemoveConsulta'),
    consultasTableBody: document.getElementById('consultasTableBody'),
    selectAllConsultas: document.getElementById('selectAllConsultas'),
    btnConsultar: document.getElementById('btnConsultar'),

    // Resultados
    resultsSection: document.getElementById('resultsSection'),
    resultsContainer: document.getElementById('resultsContainer'),

    // Modal
    loadingModal: document.getElementById('loadingModal'),
    footerStatus: document.getElementById('footerStatus'),
    toast: document.getElementById('toast')
};

// ==================== Inicialización ====================
document.addEventListener('DOMContentLoaded', () => {
    loadFromStorage();
    setupEventListeners();
    generateTransactionNumber();
});

// ==================== Event Listeners ====================
function setupEventListeners() {
    // Tabs
    elements.tabButtons.forEach(button => {
        button.addEventListener('click', (e) => switchTab(e.target.dataset.tab));
    });

    // Autenticación
    [elements.ruc, elements.usuarioSOL, elements.claveSol, elements.codigoEnvio].forEach(input => {
        input.addEventListener('change', saveToStorage);
    });
    elements.btnRefresh.addEventListener('click', loadReferentialData);

    // Envios
    elements.btnAddEnvio.addEventListener('click', addEnvio);
    elements.btnRemoveEnvio.addEventListener('click', removeSelectedEnvios);
    elements.selectAllEnvios.addEventListener('change', selectAllEnvios);
    elements.btnEnviar.addEventListener('click', submitEnvios);
    elements.archivoEnvio.addEventListener('change', () => {
        if (elements.archivoEnvio.files.length > 0) {
            const fileName = elements.archivoEnvio.files[0].name;
            // Auto-fill nombre archivo en la tabla
        }
    });

    // Consultas
    elements.btnAddConsulta.addEventListener('click', addConsulta);
    elements.btnRemoveConsulta.addEventListener('click', removeSelectedConsultas);
    elements.selectAllConsultas.addEventListener('change', selectAllConsultas);
    elements.btnConsultar.addEventListener('click', submitConsultas);
}

// ==================== Gestión de Tabs ====================
function switchTab(tabName) {
    elements.tabButtons.forEach(btn => btn.classList.remove('active'));
    elements.tabContents.forEach(content => content.classList.remove('active'));

    document.querySelector(`[data-tab="${tabName}"]`).classList.add('active');
    document.getElementById(tabName).classList.add('active');
}

// ==================== Envios ====================
function addEnvio() {
    const transactionNumber = elements.numeroTransaccion.value.trim();
    const file = elements.archivoEnvio.files[0];

    if (!transactionNumber) {
        showToast('Por favor ingresa un número de transacción', 'warning');
        return;
    }

    if (!file) {
        showToast('Por favor selecciona un archivo ZIP', 'warning');
        return;
    }

    const envio = {
        id: Date.now(),
        numeroTransaccion: transactionNumber,
        nombreArchivo: file.name,
        archivoContent: file,
        estado: 'Pendiente'
    };

    appState.envios.push(envio);
    renderEnviosTable();
    saveToStorage();

    // Limpiar inputs
    elements.numeroTransaccion.value = '';
    elements.archivoEnvio.value = '';
    generateTransactionNumber();
    showToast('Archivo agregado correctamente', 'success');
}

function removeSelectedEnvios() {
    if (appState.selectedEnvios.size === 0) {
        showToast('Selecciona archivos para eliminar', 'warning');
        return;
    }

    appState.envios = appState.envios.filter(e => !appState.selectedEnvios.has(e.id));
    appState.selectedEnvios.clear();
    renderEnviosTable();
    saveToStorage();
    showToast('Archivos eliminados', 'success');
}

function selectAllEnvios(e) {
    if (e.target.checked) {
        appState.selectedEnvios = new Set(appState.envios.map(e => e.id));
    } else {
        appState.selectedEnvios.clear();
    }
    renderEnviosTable();
}

function renderEnviosTable() {
    elements.enviosTableBody.innerHTML = appState.envios.map(envio => `
        <tr class="${appState.selectedEnvios.has(envio.id) ? 'selected' : ''}">
            <td>
                <input type="checkbox" ${appState.selectedEnvios.has(envio.id) ? 'checked' : ''}
                    onchange="toggleEnvioSelection(${envio.id})">
            </td>
            <td>${escapeHtml(envio.numeroTransaccion)}</td>
            <td>${escapeHtml(envio.nombreArchivo)}</td>
            <td><span class="status-badge pending">${envio.estado}</span></td>
        </tr>
    `).join('');

    elements.selectAllEnvios.checked = appState.selectedEnvios.size === appState.envios.length && appState.envios.length > 0;
}

function toggleEnvioSelection(id) {
    if (appState.selectedEnvios.has(id)) {
        appState.selectedEnvios.delete(id);
    } else {
        appState.selectedEnvios.add(id);
    }
    renderEnviosTable();
}

async function submitEnvios() {
    if (appState.envios.length === 0) {
        showToast('Agrega archivos para enviar', 'warning');
        return;
    }

    if (!validateAuth()) return;

    showLoading(true);
    const results = [];

    try {
        for (const envio of appState.envios) {
            const result = await submitFile(envio);
            results.push({
                numeroTransaccion: envio.numeroTransaccion,
                nombreArchivo: envio.nombreArchivo,
                ...result
            });

            // Actualizar estado
            envio.estado = result.success ? 'Completado' : 'Error';
        }

        renderEnviosTable();
        displayResults(results);
        showToast(`${results.filter(r => r.success).length}/${results.length} archivos procesados`, 'success');

    } catch (error) {
        showToast('Error procesando envíos: ' + error.message, 'error');
    } finally {
        showLoading(false);
    }
}

async function submitFile(envio) {
    try {
        // Crear FormData multipart/form-data (lo que espera IFormFile)
        const formData = new FormData();
        formData.append('file', envio.archivoContent);

        const response = await fetch(`${API_BASE_URL}/submissions/submit-with-transaction?transactionNumber=${encodeURIComponent(envio.numeroTransaccion)}`, {
            method: 'POST',
            body: formData
            // NO incluir Content-Type: FormData lo establece automáticamente con boundary
        });

        if (!response.ok) {
            const errorData = await response.text();
            throw new Error(errorData || `HTTP ${response.status}`);
        }

        const data = await response.json();
        return {
            success: true,
            ticketNumber: data.ticketNumber,
            errors: data.errors || [],
            warnings: data.warnings || []
        };
    } catch (error) {
        return {
            success: false,
            error: error.message
        };
    }
}

// ==================== Consultas ====================
function addConsulta() {
    const anno = elements.annoConsulta.value.trim();
    const numeroTicket = elements.numeroTicket.value.trim();

    if (!anno || !numeroTicket) {
        showToast('Por favor completa año y número de ticket', 'warning');
        return;
    }

    const consulta = {
        id: Date.now(),
        anno: anno,
        numeroTicket: numeroTicket,
        estado: 'Pendiente'
    };

    appState.consultas.push(consulta);
    renderConsultasTable();
    saveToStorage();

    elements.numeroTicket.value = '';
    showToast('Consulta agregada correctamente', 'success');
}

function removeSelectedConsultas() {
    if (appState.selectedConsultas.size === 0) {
        showToast('Selecciona consultas para eliminar', 'warning');
        return;
    }

    appState.consultas = appState.consultas.filter(c => !appState.selectedConsultas.has(c.id));
    appState.selectedConsultas.clear();
    renderConsultasTable();
    saveToStorage();
    showToast('Consultas eliminadas', 'success');
}

function selectAllConsultas(e) {
    if (e.target.checked) {
        appState.selectedConsultas = new Set(appState.consultas.map(c => c.id));
    } else {
        appState.selectedConsultas.clear();
    }
    renderConsultasTable();
}

function renderConsultasTable() {
    elements.consultasTableBody.innerHTML = appState.consultas.map(consulta => `
        <tr class="${appState.selectedConsultas.has(consulta.id) ? 'selected' : ''}">
            <td>
                <input type="checkbox" ${appState.selectedConsultas.has(consulta.id) ? 'checked' : ''}
                    onchange="toggleConsultaSelection(${consulta.id})">
            </td>
            <td>${escapeHtml(consulta.anno)}</td>
            <td>${escapeHtml(consulta.numeroTicket)}</td>
            <td><span class="status-badge pending">${consulta.estado}</span></td>
        </tr>
    `).join('');

    elements.selectAllConsultas.checked = appState.selectedConsultas.size === appState.consultas.length && appState.consultas.length > 0;
}

function toggleConsultaSelection(id) {
    if (appState.selectedConsultas.has(id)) {
        appState.selectedConsultas.delete(id);
    } else {
        appState.selectedConsultas.add(id);
    }
    renderConsultasTable();
}

async function submitConsultas() {
    if (appState.consultas.length === 0) {
        showToast('Agrega consultas para procesar', 'warning');
        return;
    }

    if (!validateAuth()) return;

    showLoading(true);
    const results = [];

    try {
        for (const consulta of appState.consultas) {
            const result = await queryStatus(consulta);
            results.push({
                anno: consulta.anno,
                numeroTicket: consulta.numeroTicket,
                ...result
            });

            consulta.estado = result.success ? 'Completado' : 'Error';
        }

        renderConsultasTable();
        displayResults(results);
        showToast(`${results.filter(r => r.success).length}/${results.length} consultas procesadas`, 'success');

    } catch (error) {
        showToast('Error procesando consultas: ' + error.message, 'error');
    } finally {
        showLoading(false);
    }
}

async function queryStatus(consulta) {
    try {
        const params = new URLSearchParams({
            year: consulta.anno,
            ticketNumber: consulta.numeroTicket
        });

        const response = await fetch(`/api/queries/query-status?${params}`, {
            method: 'GET',
            headers: {'Content-Type': 'application/json'}
        });

        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }

        const data = await response.json();

        return {
            success: true,
            statusType: data.statusType,
            description: data.description,
            errorCode: data.errorCode || '',
            errorMessage: data.errorMessage || ''
        };
    } catch (error) {
        return {
            success: false,
            error: error.message,
            errorCode: '',
            errorMessage: ''
        };
    }
}

// ==================== Resultados ====================
function displayResults(results) {
    elements.resultsContainer.innerHTML = results.map((result, idx) => {
        const success = result.success;
        const cssClass = success ? 'success' : 'error';

        let errorSection = '';
        if (result.errorCode || result.errorMessage) {
            errorSection = `
                <hr>
                <p style="color: #d32f2f; font-weight: bold;">⚠ Información de Error SUNAT:</p>
                ${result.errorCode ? `<p><strong>Código:</strong> ${escapeHtml(result.errorCode)}</p>` : ''}
                ${result.errorMessage ? `<p><strong>Mensaje:</strong> ${escapeHtml(result.errorMessage)}</p>` : ''}
            `;
        }

        return `
            <div class="result-item ${cssClass}">
                <h4>#${idx + 1} - ${success ? '✓ Éxito' : '✗ Error'}</h4>
                <p><strong>Transacción/Ticket:</strong> ${escapeHtml(result.numeroTransaccion || result.anno + '-' + result.numeroTicket)}</p>
                <p><strong>Archivo/Año:</strong> ${escapeHtml(result.nombreArchivo || result.anno)}</p>
                <p><strong>Tipo de Respuesta:</strong> ${escapeHtml(result.statusType || '')}</p>
                <p><strong>Descripción:</strong> ${escapeHtml(result.description || '(sin descripción)')}</p>
                ${success ? `
                    <p><strong>Error HTTP:</strong> ${escapeHtml(result.error || 'Ninguno')}</p>
                ` : `
                    <p><strong>Error HTTP:</strong> ${escapeHtml(result.error || '')}</p>
                `}
                ${errorSection}
            </div>
        `;
    }).join('');

    elements.resultsSection.style.display = 'block';
    elements.resultsSection.scrollIntoView({ behavior: 'smooth' });
}

// ==================== Validación ====================
function validateAuth() {
    const ruc = elements.ruc.value.trim();
    const usuarioSOL = elements.usuarioSOL.value.trim();
    const claveSol = elements.claveSol.value.trim();

    if (!ruc || !usuarioSOL || !claveSol) {
        showToast('Por favor completa los datos de autenticación', 'warning');
        return false;
    }

    if (ruc.length !== 11 || !/^\d+$/.test(ruc)) {
        showToast('RUC debe tener 11 dígitos', 'warning');
        return false;
    }

    return true;
}

// ==================== Datos Referenciales ====================
function loadReferentialData() {
    showLoading(true);

    // Simulación de carga de datos
    // En producción, esto vendría de un endpoint del API
    setTimeout(() => {
        elements.ipAddress.textContent = '169.254.123.253';
        elements.macAddress.textContent = '00:15:50:60:7B:00';
        showLoading(false);
        showToast('Datos referenciales actualizados', 'success');
    }, 500);
}

// ==================== Generación de Transacción ====================
function generateTransactionNumber() {
    const ruc = elements.ruc.value.trim() || '00000000000';
    if (ruc.length !== 11) return;

    const now = new Date();
    const date = now.toISOString().split('T')[0].replace(/-/g, '');
    const time = now.toTimeString().split(' ')[0].replace(/:/g, '');
    const sequence = Math.floor(Math.random() * 1000).toString().padStart(3, '0');

    elements.numeroTransaccion.value = `${ruc}-${date}-${time}-${sequence}`;
    elements.numeroTransaccion.readOnly = true;
}

elements.ruc.addEventListener('change', generateTransactionNumber);

// ==================== Utilidades ====================
function readFileAsArrayBuffer(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsArrayBuffer(file);
    });
}

function readFileAsBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

function showLoading(show) {
    appState.isProcessing = show;
    elements.loadingModal.classList.toggle('active', show);
    elements.btnEnviar.disabled = show;
    elements.btnConsultar.disabled = show;
    elements.footerStatus.textContent = show ? 'Procesando...' : 'Listo';
}

function showToast(message, type = 'info') {
    elements.toast.textContent = message;
    elements.toast.className = `toast show ${type}`;

    setTimeout(() => {
        elements.toast.classList.remove('show');
    }, 4000);
}

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

// ==================== Persistencia ====================
function saveToStorage() {
    appState.auth = {
        ruc: elements.ruc.value,
        usuarioSOL: elements.usuarioSOL.value,
        claveSol: elements.claveSol.value,
        codigoEnvio: elements.codigoEnvio.value
    };

    localStorage.setItem(STORAGE_KEY_AUTH, JSON.stringify(appState.auth));
}

function loadFromStorage() {
    const auth = localStorage.getItem(STORAGE_KEY_AUTH);
    if (auth) {
        appState.auth = JSON.parse(auth);
        elements.ruc.value = appState.auth.ruc;
        elements.usuarioSOL.value = appState.auth.usuarioSOL;
        elements.claveSol.value = appState.auth.claveSol;
        elements.codigoEnvio.value = appState.auth.codigoEnvio;
    }
}

// ==================== Configuración de CORS ====================
// Para desarrollo local
if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
    console.log('Modo desarrollo - CORS habilitado');
}

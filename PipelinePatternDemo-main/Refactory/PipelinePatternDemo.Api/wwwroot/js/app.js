// Script do front-end: monta a proposta a partir do formulário, chama a API
// e mostra a decisão do pipeline na tela de forma amigável.

const API_URL = '/api/loan-evaluation';

// Casos prontos para testar rapidamente cada cenário (reprovado, com avisos, aprovado).
const DEMO_CASES = [
    {
        applicantName: 'John Doe', creditScore: 550, annualIncome: 30000,
        monthlyDebt: 500, requestedLoanAmount: 250000, employmentYears: 3,
        hasExistingLoans: true, isFlaggedForFraud: false
    },
    {
        applicantName: 'Jane Smith', creditScore: 720, annualIncome: 85000,
        monthlyDebt: 1200, requestedLoanAmount: 200000, employmentYears: 5,
        hasExistingLoans: true, isFlaggedForFraud: true
    },
    {
        applicantName: 'Bob Johnson', creditScore: 650, annualIncome: 75000,
        monthlyDebt: 800, requestedLoanAmount: 150000, employmentYears: 1.5,
        hasExistingLoans: false, isFlaggedForFraud: false
    },
    {
        applicantName: 'Alice Brown', creditScore: 780, annualIncome: 120000,
        monthlyDebt: 1000, requestedLoanAmount: 200000, employmentYears: 6,
        hasExistingLoans: false, isFlaggedForFraud: false
    },
    {
        applicantName: 'Mike Wilson', creditScore: 680, annualIncome: 45000,
        monthlyDebt: 1500, requestedLoanAmount: 300000, employmentYears: 4,
        hasExistingLoans: true, isFlaggedForFraud: false
    },
    {
        applicantName: 'Sarah Chen', creditScore: 700, annualIncome: 90000,
        monthlyDebt: 2000, requestedLoanAmount: 180000, employmentYears: 3,
        hasExistingLoans: true, isFlaggedForFraud: false
    }
];

document.querySelectorAll('.example-card').forEach(card => {
    card.addEventListener('click', () => {
        const data = DEMO_CASES[card.dataset.case];
        if (!data) return;

        document.getElementById('applicantName').value = data.applicantName;
        document.getElementById('creditScore').value = data.creditScore;
        document.getElementById('annualIncome').value = data.annualIncome;
        document.getElementById('monthlyDebt').value = data.monthlyDebt;
        document.getElementById('requestedLoanAmount').value = data.requestedLoanAmount;
        document.getElementById('employmentYears').value = data.employmentYears;
        document.getElementById('hasExistingLoans').checked = data.hasExistingLoans;
        document.getElementById('isFlaggedForFraud').checked = data.isFlaggedForFraud;

        form.dispatchEvent(new SubmitEvent('submit', { cancelable: true }));
    });
});

const form = document.getElementById('loanForm');
const submitBtn = document.getElementById('submitBtn');
const btnText = submitBtn.querySelector('.btn-text');
const btnLoading = submitBtn.querySelector('.btn-loading');
const resultDiv = document.getElementById('result');
const errorDiv = document.getElementById('error');

form.addEventListener('submit', async (e) => {
    e.preventDefault();
    hideResults();

    const body = {
        applicantName: document.getElementById('applicantName').value.trim(),
        creditScore: Number(document.getElementById('creditScore').value),
        annualIncome: Number(document.getElementById('annualIncome').value),
        monthlyDebt: Number(document.getElementById('monthlyDebt').value),
        requestedLoanAmount: Number(document.getElementById('requestedLoanAmount').value),
        employmentYears: Number(document.getElementById('employmentYears').value),
        hasExistingLoans: document.getElementById('hasExistingLoans').checked,
        isFlaggedForFraud: document.getElementById('isFlaggedForFraud').checked
    };

    setLoading(true);

    try {
        const res = await fetch(API_URL, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        // 400 = dados inválidos: mostramos as mensagens das DataAnnotations para o usuário corrigir.
        if (res.status === 400) {
            const problem = await res.json().catch(() => null);
            const messages = problem && problem.errors
                ? Object.values(problem.errors).flat()
                : ['Dados inválidos.'];
            throw new Error(messages.join(' '));
        }

        if (!res.ok) {
            throw new Error(`Erro no servidor: ${res.status}`);
        }

        const data = await res.json();
        showResult(data);
    } catch (err) {
        showError(err.message);
    } finally {
        setLoading(false);
    }
});

function setLoading(loading) {
    submitBtn.disabled = loading;
    btnText.hidden = loading;
    btnLoading.hidden = !loading;
}

function showResult(data) {
    const header = document.getElementById('resultHeader');
    const icon = document.getElementById('resultIcon');
    const title = document.getElementById('resultTitle');

    // Cada status tem sua cor, ícone e título. PendingReview = falha técnica, a proposta vai para revisão.
    const view = {
        Approved: { css: 'approved', icon: '✅', title: 'PROPOSTA APROVADA' },
        Rejected: { css: 'rejected', icon: '❌', title: 'PROPOSTA REPROVADA' },
        PendingReview: { css: 'pending', icon: '⏳', title: 'PROPOSTA PENDENTE DE REVISÃO' }
    }[data.status] || { css: 'rejected', icon: '❌', title: 'PROPOSTA REPROVADA' };

    header.className = 'result-header ' + view.css;
    icon.textContent = view.icon;
    title.textContent = view.title;

    document.getElementById('applicationId').textContent = data.applicationId;
    document.getElementById('resultApplicantName').textContent = data.applicantName;

    renderList('rejectionSection', 'rejectionList', data.rejectionReasons);
    renderList('warningSection', 'warningList', data.warnings);
    renderList('systemErrorSection', 'systemErrorList', data.systemErrors);

    resultDiv.hidden = false;
}

function renderList(sectionId, listId, items) {
    const section = document.getElementById(sectionId);
    const list = document.getElementById(listId);

    if (items && items.length > 0) {
        list.innerHTML = items.map(item => `<li>${escapeHtml(item)}</li>`).join('');
        section.hidden = false;
    } else {
        section.hidden = true;
    }
}

function showError(message) {
    document.getElementById('errorMessage').textContent = message;
    errorDiv.hidden = false;
}

function hideResults() {
    resultDiv.hidden = true;
    errorDiv.hidden = true;
}

// Evita que textos vindos da API sejam interpretados como HTML (proteção contra XSS).
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

const API_URL = '/api/loan-evaluation';

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

        if (!res.ok) {
            throw new Error(`Server error: ${res.status}`);
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

    header.className = 'result-header ' + (data.isApproved ? 'approved' : 'rejected');
    icon.textContent = data.isApproved ? '✅' : '❌';
    title.textContent = data.isApproved ? 'APPLICATION APPROVED' : 'APPLICATION REJECTED';

    document.getElementById('applicationId').textContent = data.applicationId;
    document.getElementById('resultApplicantName').textContent = data.applicantName;

    renderList('rejectionSection', 'rejectionList', data.rejectionReasons);
    renderList('warningSection', 'warningList', data.warnings);

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

function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

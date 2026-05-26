import React, { useState, useEffect } from 'react';
import axios from 'axios';
import { 
  CreditCard, 
  Wallet, 
  Zap, 
  ShieldCheck, 
  Coins, 
  ChevronRight,
  X,
  Info,
  CheckCircle2,
  AlertCircle
} from 'lucide-react';

const API_BASE = 'http://localhost:5001/api';

function App() {
  const [packages, setPackages] = useState([]);
  const [devPackages, setDevPackages] = useState([]);
  const [balance, setBalance] = useState(0);
  const [isDevMode, setIsDevMode] = useState(false);
  const [devClickCount, setDevClickCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [loadingText, setLoadingText] = useState('Đang xử lý...');
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);
  
  // Custom Toast State
  const [toast, setToast] = useState({ show: false, message: '', type: 'success' });

  const showToast = (message, type = 'success') => {
    setToast({ show: true, message, type });
    setTimeout(() => setToast({ show: false, message: '', type: 'success' }), 4000);
  };

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchBalance, 3000);
    
    // Check for payment result in URL
    const urlParams = new URLSearchParams(window.location.search);
    const status = urlParams.get('paymentStatus'); // From our VNPay redirect
    const momoResult = urlParams.get('resultCode'); // From MoMo direct redirect

    if (status || momoResult) {
      if (status === 'success' || momoResult === '0') {
        showToast('Thanh toán thành công! Cảm ơn bạn đã ủng hộ.', 'success');
      } else if (status === 'failed' || (momoResult && momoResult !== '0')) {
        showToast('Giao dịch thất bại hoặc đã bị hủy.', 'error');
      } else if (status === 'error') {
        showToast('Lỗi xử lý: ' + urlParams.get('message'), 'error');
      }
      // Clear query params
      window.history.replaceState({}, document.title, window.location.pathname);
      fetchBalance();
      fetchData();
    }

    return () => clearInterval(interval);
  }, [isDevMode]);

  const fetchData = async () => {
    try {
      const res = await axios.get(`${API_BASE}/packages?dev=${isDevMode}`);
      setPackages(res.data.packages);
      setDevPackages(res.data.devPackages);
      fetchBalance();
    } catch (err) {
      console.error('Fetch error:', err);
    }
  };

  const fetchBalance = async () => {
    try {
      const res = await axios.get(`${API_BASE}/user/balance`);
      setBalance(res.data.balance);
    } catch (err) {}
  };

  const handleDevToggle = () => {
    setDevClickCount(prev => {
      if (prev + 1 >= 5) {
        setIsDevMode(!isDevMode);
        showToast(isDevMode ? 'Đã tắt Dev Mode' : 'Đã bật Dev Mode!', 'success');
        return 0;
      }
      return prev + 1;
    });
  };

  const initiatePayment = async (packageId, provider) => {
    setLoading(true);
    setLoadingText('Đang xử lý...');
    try {
      let endpoint = '';
      if (provider === 'vnpay') endpoint = '/payment/vnpay';
      else if (provider === 'momo') endpoint = '/payment/momo';
      else if (provider === 'momo_atm') endpoint = '/payment/momo_atm';
      else if (provider === 'dev') endpoint = '/payment/dev_recharge';
      else if (provider === 'free') endpoint = '/payment/free';

      const res = await axios.post(`${API_BASE}${endpoint}`, { packageId });
      
      if (provider === 'dev' || provider === 'free') {
        showToast(res.data.message, res.data.success !== false ? 'success' : 'error');
        fetchBalance();
        setLoading(false);
        setShowPaymentModal(false);
      } else if (res.data.paymentUrl && res.data.paymentUrl.includes('paymentStatus=')) {
        // Backend bypassed gateway (e.g. 0đ price) and returned local url directly
        setLoadingText('Đang xác nhận khuyến mãi...');
        setTimeout(() => {
            window.location.href = res.data.paymentUrl;
        }, 1500);
      } else {
        // Redirect directly to payment URL for Web
        window.location.href = res.data.paymentUrl;
      }
    } catch (err) {
      showToast('Lỗi kết nối đến máy chủ thanh toán.', 'error');
      setLoading(false);
      setShowPaymentModal(false);
    }
  };

  const renderPackage = (pkg, isDev = false) => (
    <div
      key={pkg.id}
      className={`package-card ${isDev ? 'dev-card' : ''} ${pkg.price === 0 ? 'free-card' : ''}`}
      onClick={() => {
        setSelectedPackage(pkg);
        if (!isDev) setShowPaymentModal(true);
        else initiatePayment(pkg.id, 'dev');
      }}
    >      <div className="package-icon">
        <Coins color={isDev ? "#ff00ff" : (pkg.price === 0 ? "#4CAF50" : "#ffd700")} size={28} />
      </div>
      <div className="package-info">
        <div className="package-name">{pkg.name}</div>
        <div className="package-desc">{pkg.description}</div>
      </div>
      <div className="package-price-section">
        <div className={`price-text ${pkg.price === 0 ? 'price-free' : ''}`}>
          {pkg.price === 0 && pkg.id === 'p0' ? (
            <>
              <span style={{ textDecoration: 'line-through', color: '#8a8a9d', fontSize: '14px', marginRight: '6px', fontWeight: 'normal' }}>29.000đ</span>
              0đ
            </>
          ) : pkg.price === 0 ? 'MIỄN PHÍ' : pkg.price.toLocaleString() + 'đ'}
        </div>
        <ChevronRight className="chevron-icon" size={20} />
      </div>
    </div>
  );

  return (
    <div className="mobile-container">
      {/* Toast Notification */}
      <div className={`toast-container ${toast.show ? 'toast-visible' : ''}`}>
        <div className={`toast ${toast.type}`}>
          {toast.type === 'success' ? <CheckCircle2 size={20} /> : <AlertCircle size={20} />}
          <span>{toast.message}</span>
        </div>
      </div>

      {/* Header */}
      <header className="header">
        <div onClick={handleDevToggle} className="logo">
          <span className="logo-accent">RHYTHM</span> STORE
        </div>
        <div className="balance-pill">
          <Coins color="#ffd700" size={16} />
          <span>{balance.toLocaleString()}</span>
        </div>
      </header>

      <main className="main-content">
        <div className="promo-banner">
          <div className="promo-icon-bg">
            <Zap color="#fff" size={28} />
          </div>
          <div className="promo-text">
            <h3>Ưu đãi đặc biệt</h3>
            <p>X2 Xu cho lần nạp đầu tiên qua VNPay!</p>
          </div>
        </div>

        <div className="section-label">GÓI NẠP TÀI KHOẢN</div>
        <div className="packages-grid">
          {packages.map(pkg => renderPackage(pkg))}
        </div>

        {isDevMode && (
          <>
            <div className="section-label dev-label">DEV TOOLS (TEST ONLY)</div>
            <div className="packages-grid">
              {devPackages.map(pkg => renderPackage(pkg, true))}
            </div>
          </>
        )}

        <footer className="footer-info">
          <div className="info-badge">
            <ShieldCheck color="#4CAF50" size={16} />
            <span>Giao dịch mã hóa SSL 256-bit</span>
          </div>
          <div className="info-badge">
            <Info color="#888" size={16} />
            <span>Hỗ trợ khách hàng: support@rhythm.game</span>
          </div>
        </footer>
      </main>

      {/* Payment Modal */}
      {showPaymentModal && (
        <div className="modal-overlay" onClick={() => setShowPaymentModal(false)}>
          <div className="modal-content" onClick={e => e.stopPropagation()}>
            <div className="modal-header">
              <h3>Chọn cổng thanh toán</h3>
              <button className="close-btn" onClick={() => setShowPaymentModal(false)}>
                <X size={24} />
              </button>
            </div>
            
            <div className="modal-body">
              <div className="order-summary">
                <span className="summary-label">Đang thanh toán cho:</span>
                <span className="summary-value">{selectedPackage?.name}</span>
                <div className="summary-price">
                  {selectedPackage?.price === 0 && selectedPackage?.id === 'p0' ? (
                    <>
                      <span style={{ textDecoration: 'line-through', color: '#8a8a9d', fontSize: '18px', marginRight: '8px', fontWeight: 'normal' }}>29.000đ</span>
                      0đ
                    </>
                  ) : (
                    selectedPackage?.price.toLocaleString() + 'đ'
                  )}
                </div>
              </div>
              
              <button className="method-btn vnpay" onClick={() => initiatePayment(selectedPackage.id, 'vnpay')}>
                <div className="method-icon"><CreditCard size={24} color="#005BAA" /></div>
                <div className="method-details">
                  <span className="method-title">Thẻ ATM / VNPay</span>
                  <span className="method-desc">Hỗ trợ mọi ngân hàng nội địa</span>
                </div>
                <ChevronRight size={20} color="#ccc" />
              </button>

              <button className="method-btn momo" onClick={() => initiatePayment(selectedPackage.id, 'momo')}>
                <div className="method-icon"><Wallet size={24} color="#A50064" /></div>
                <div className="method-details">
                  <span className="method-title">Ví MoMo</span>
                  <span className="method-desc">Quét mã QR nhanh chóng</span>
                </div>
                <ChevronRight size={20} color="#ccc" />
              </button>

              <button className="method-btn momo" onClick={() => initiatePayment(selectedPackage.id, 'momo_atm')}>
                <div className="method-icon"><CreditCard size={24} color="#A50064" /></div>
                <div className="method-details">
                  <span className="method-title">Thẻ ATM qua MoMo</span>
                  <span className="method-desc">Hỗ trợ ngân hàng nội địa</span>
                </div>
                <ChevronRight size={20} color="#ccc" />
              </button>
            </div>
          </div>
        </div>
      )}

      {loading && (
        <div className="loader-overlay">
          <div className="loader-box">
            <div className="loader"></div>
            <span>{loadingText}</span>
          </div>
        </div>
      )}

      <style>{`
        :root {
          --bg-dark: #0a0a0f;
          --bg-card: #15151e;
          --bg-card-hover: #1c1c28;
          --primary: #ffd700;
          --text-main: #ffffff;
          --text-muted: #8a8a9d;
          --border-color: rgba(255, 255, 255, 0.05);
          --success: #4CAF50;
          --error: #f44336;
        }

        body {
          margin: 0;
          background: var(--bg-dark);
          color: var(--text-main);
          font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, Cantarell, sans-serif;
          -webkit-font-smoothing: antialiased;
        }

        .mobile-container {
          max-width: 480px;
          margin: 0 auto;
          background: var(--bg-dark);
          min-height: 100vh;
          position: relative;
          box-shadow: 0 0 40px rgba(0,0,0,0.5);
        }

        /* Toast */
        .toast-container {
          position: fixed;
          top: 20px;
          left: 50%;
          transform: translateX(-50%) translateY(-150%);
          z-index: 1000;
          transition: transform 0.4s cubic-bezier(0.175, 0.885, 0.32, 1.275);
          pointer-events: none;
          width: 90%;
          max-width: 400px;
        }
        .toast-visible {
          transform: translateX(-50%) translateY(0);
        }
        .toast {
          display: flex;
          align-items: center;
          gap: 12px;
          padding: 14px 20px;
          border-radius: 12px;
          font-weight: 600;
          font-size: 14px;
          box-shadow: 0 10px 30px rgba(0,0,0,0.5);
          backdrop-filter: blur(10px);
        }
        .toast.success { background: rgba(76, 175, 80, 0.9); border: 1px solid #4CAF50; }
        .toast.error { background: rgba(244, 67, 54, 0.9); border: 1px solid #f44336; }

        /* Header */
        .header {
          display: flex;
          justify-content: space-between;
          align-items: center;
          padding: 20px 24px;
          background: rgba(10, 10, 15, 0.8);
          backdrop-filter: blur(10px);
          position: sticky;
          top: 0;
          z-index: 10;
          border-bottom: 1px solid var(--border-color);
        }
        .logo {
          font-weight: 900;
          font-size: 18px;
          letter-spacing: 1px;
          cursor: pointer;
          user-select: none;
        }
        .logo-accent { color: var(--primary); }
        .balance-pill {
          background: rgba(255, 215, 0, 0.1);
          border: 1px solid rgba(255, 215, 0, 0.2);
          padding: 6px 14px;
          border-radius: 20px;
          display: flex;
          align-items: center;
          gap: 8px;
          color: var(--primary);
          font-weight: 700;
          box-shadow: 0 0 15px rgba(255, 215, 0, 0.1);
        }

        .main-content {
          padding: 24px;
          padding-bottom: 80px;
        }

        /* Promo Banner */
        .promo-banner {
          background: linear-gradient(135deg, #1e1e2d 0%, #2a2a3d 100%);
          border-radius: 16px;
          padding: 20px;
          display: flex;
          align-items: center;
          gap: 16px;
          margin-bottom: 30px;
          position: relative;
          overflow: hidden;
          border: 1px solid rgba(255, 215, 0, 0.1);
          box-shadow: 0 8px 20px rgba(0,0,0,0.2);
        }
        .promo-banner::before {
          content: '';
          position: absolute;
          top: 0; left: 0; width: 4px; height: 100%;
          background: var(--primary);
        }
        .promo-icon-bg {
          background: linear-gradient(135deg, #ff9800, #ff5722);
          width: 48px; height: 48px;
          border-radius: 12px;
          display: flex; align-items: center; justify-content: center;
          box-shadow: 0 4px 10px rgba(255, 152, 0, 0.3);
        }
        .promo-text h3 { margin: 0; font-size: 16px; font-weight: 800; color: #fff; }
        .promo-text p { margin: 4px 0 0; font-size: 13px; color: #d1d1d6; }
        
        .section-label {
          font-size: 12px;
          font-weight: 800;
          color: var(--text-muted);
          margin-bottom: 16px;
          letter-spacing: 1.5px;
          text-transform: uppercase;
        }
        .dev-label { color: #ff00ff; margin-top: 32px; }

        /* Packages Grid */
        .packages-grid {
          display: flex;
          flex-direction: column;
          gap: 12px;
        }
        .package-card {
          background: var(--bg-card);
          border-radius: 16px;
          padding: 16px;
          display: flex;
          align-items: center;
          cursor: pointer;
          transition: all 0.2s ease;
          border: 1px solid var(--border-color);
        }
        .package-card:hover {
          background: var(--bg-card-hover);
          border-color: rgba(255, 215, 0, 0.3);
          transform: translateY(-2px);
        }
        .package-card:active { transform: scale(0.98); }
        
        .free-card {
          background: linear-gradient(to right, var(--bg-card), rgba(76, 175, 80, 0.05));
          border: 1px solid rgba(76, 175, 80, 0.2);
        }
        .dev-card { border: 1px dashed rgba(255, 0, 255, 0.4); }
        
        .package-icon {
          background: rgba(0, 0, 0, 0.2);
          width: 48px; height: 48px;
          border-radius: 12px;
          display: flex; align-items: center; justify-content: center;
        }
        .package-info { flex: 1; margin-left: 16px; }
        .package-name { font-weight: 700; font-size: 16px; color: #fff; }
        .package-desc { font-size: 13px; color: var(--text-muted); margin-top: 4px; }
        
        .package-price-section { display: flex; align-items: center; gap: 8px; }
        .price-text { 
          font-weight: 800; 
          font-size: 16px; 
          background: #fff; 
          -webkit-background-clip: text; 
          -webkit-text-fill-color: transparent; 
        }
        .price-free {
          background: var(--success);
          -webkit-background-clip: text; 
        }
        .chevron-icon { color: var(--text-muted); transition: color 0.2s; }
        .package-card:hover .chevron-icon { color: var(--primary); }

        /* Footer */
        .footer-info {
          margin-top: 40px;
          display: flex;
          flex-direction: column;
          align-items: center;
          gap: 12px;
          padding-top: 24px;
          border-top: 1px dashed var(--border-color);
        }
        .info-badge { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-muted); }

        /* Modal */
        .modal-overlay {
          position: fixed; inset: 0;
          background: rgba(0,0,0,0.7);
          backdrop-filter: blur(5px);
          display: flex; align-items: flex-end; justify-content: center;
          z-index: 100;
          animation: fadeIn 0.2s ease-out;
        }
        @keyframes fadeIn { from { opacity: 0; } to { opacity: 1; } }

        .modal-content {
          background: var(--bg-card);
          width: 100%; max-width: 480px;
          border-radius: 24px 24px 0 0;
          padding: 24px;
          box-shadow: 0 -10px 40px rgba(0,0,0,0.5);
          animation: slideUp 0.3s cubic-bezier(0.16, 1, 0.3, 1);
          border-top: 1px solid var(--border-color);
        }
        @keyframes slideUp { from { transform: translateY(100%); } to { transform: translateY(0); } }
        
        .modal-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 24px; }
        .modal-header h3 { margin: 0; font-size: 18px; font-weight: 800; }
        .close-btn { 
          background: rgba(255,255,255,0.05); border: none; color: var(--text-muted); 
          cursor: pointer; width: 32px; height: 32px; border-radius: 50%;
          display: flex; align-items: center; justify-content: center;
          transition: background 0.2s;
        }
        .close-btn:hover { background: rgba(255,255,255,0.1); color: #fff; }

        .order-summary {
          background: rgba(0,0,0,0.2);
          border-radius: 12px;
          padding: 16px;
          margin-bottom: 24px;
          text-align: center;
          border: 1px solid var(--border-color);
        }
        .summary-label { display: block; font-size: 13px; color: var(--text-muted); margin-bottom: 4px; }
        .summary-value { font-weight: bold; font-size: 16px; color: #fff; }
        .summary-price { font-size: 24px; font-weight: 900; color: var(--primary); margin-top: 8px; }

        .method-btn {
          width: 100%;
          padding: 16px;
          border-radius: 16px;
          border: 1px solid var(--border-color);
          background: rgba(255,255,255,0.02);
          display: flex;
          align-items: center;
          gap: 16px;
          cursor: pointer;
          margin-bottom: 12px;
          transition: all 0.2s;
          text-align: left;
        }
        .method-btn:hover {
          background: rgba(255,255,255,0.05);
          border-color: rgba(255,255,255,0.1);
          transform: translateY(-1px);
        }
        .method-icon {
          width: 48px; height: 48px;
          background: #fff;
          border-radius: 12px;
          display: flex; align-items: center; justify-content: center;
          box-shadow: 0 4px 10px rgba(0,0,0,0.2);
        }
        .method-details { flex: 1; }
        .method-title { display: block; font-size: 16px; font-weight: 700; color: #fff; margin-bottom: 4px; }
        .method-desc { display: block; font-size: 13px; color: var(--text-muted); }

        /* Loader */
        .loader-overlay {
          position: fixed; inset: 0;
          background: rgba(0,0,0,0.8);
          backdrop-filter: blur(4px);
          display: flex; align-items: center; justify-content: center;
          z-index: 200;
        }
        .loader-box {
          background: var(--bg-card);
          padding: 24px 40px;
          border-radius: 16px;
          display: flex; flex-direction: column; align-items: center; gap: 16px;
          border: 1px solid var(--border-color);
          box-shadow: 0 20px 40px rgba(0,0,0,0.4);
        }
        .loader-box span { font-weight: 600; color: var(--primary); }
        .loader {
          width: 36px; height: 36px;
          border: 3px solid rgba(255, 215, 0, 0.2);
          border-top-color: var(--primary);
          border-radius: 50%;
          animation: spin 0.8s linear infinite;
        }
        @keyframes spin { to { transform: rotate(360deg); } }
      `}</style>
    </div>
  );
}

export default App;
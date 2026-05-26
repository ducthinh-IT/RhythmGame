import React, { useState, useEffect } from 'react';
import { 
  StyleSheet, 
  Text, 
  View, 
  ScrollView, 
  TouchableOpacity, 
  SafeAreaView, 
  Modal, 
  ActivityIndicator,
  Alert,
  Platform
} from 'react-native';
import { WebView } from 'react-native-webview';
import axios from 'axios';
import { StatusBar } from 'expo-status-bar';
import { CreditCard, Wallet, Zap, ShieldCheck, AlertCircle, Coins, ChevronRight } from 'lucide-react-native';

const API_BASE = 'http://localhost:5000/api'; // Change to your IP if testing on real device

export default function App() {
  const [packages, setPackages] = useState([]);
  const [devPackages, setDevPackages] = useState([]);
  const [balance, setBalance] = useState(0);
  const [isDevMode, setIsDevMode] = useState(false);
  const [devClickCount, setDevClickCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [paymentUrl, setPaymentUrl] = useState(null);
  const [selectedPackage, setSelectedPackage] = useState(null);
  const [showPaymentModal, setShowPaymentModal] = useState(false);

  useEffect(() => {
    fetchData();
    const interval = setInterval(fetchBalance, 5000);
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
        Alert.alert('System', isDevMode ? 'Dev Mode Disabled' : 'Dev Mode Enabled! Packages unlocked.');
        return 0;
      }
      return prev + 1;
    });
  };

  const initiatePayment = async (packageId, provider) => {
    setLoading(true);
    try {
      let endpoint = '';
      if (provider === 'vnpay') endpoint = '/payment/vnpay';
      else if (provider === 'momo') endpoint = '/payment/momo';
      else if (provider === 'dev') endpoint = '/payment/dev_recharge';

      const res = await axios.post(`${API_BASE}${endpoint}`, { packageId });
      
      if (provider === 'dev') {
        Alert.alert(res.data.success ? 'Success' : 'Failed', res.data.message);
        fetchBalance();
      } else {
        setPaymentUrl(res.data.paymentUrl);
      }
    } catch (err) {
      Alert.alert('Error', 'Không thể kết nối đến máy chủ thanh toán.');
    } finally {
      setLoading(false);
      setShowPaymentModal(false);
    }
  };

  const renderPackage = (pkg, isDev = false) => (
    <TouchableOpacity 
      key={pkg.id} 
      style={[styles.packageCard, isDev && styles.devCard]}
      onPress={() => {
        setSelectedPackage(pkg);
        if (!isDev) setShowPaymentModal(true);
        else initiatePayment(pkg.id, 'dev');
      }}
    >
      <View style={styles.packageIcon}>
        <Coins color={isDev ? "#ff00ff" : "#ffd700"} size={32} />
      </View>
      <View style={styles.packageInfo}>
        <Text style={styles.packageName}>{pkg.name}</Text>
        <Text style={styles.packageDesc}>{pkg.description}</Text>
      </View>
      <View style={styles.packagePrice}>
        <Text style={styles.priceText}>{pkg.price === 0 ? 'FREE' : pkg.price.toLocaleString() + 'đ'}</Text>
        <ChevronRight color="#666" size={20} />
      </View>
    </TouchableOpacity>
  );

  return (
    <SafeAreaView style={styles.container}>
      <StatusBar style="light" />
      
      {/* Header */}
      <View style={styles.header}>
        <TouchableOpacity onPress={handleDevToggle} activeOpacity={1}>
          <Text style={styles.headerTitle}>RHYTHM STORE</Text>
        </TouchableOpacity>
        <View style={styles.balanceContainer}>
          <Coins color="#ffd700" size={20} />
          <Text style={styles.balanceText}>{balance.toLocaleString()}</Text>
        </View>
      </View>

      <ScrollView contentContainerStyle={styles.scrollContent}>
        <View style={styles.banner}>
          <Zap color="#ffd700" size={40} />
          <View>
            <Text style={styles.bannerTitle}>Nạp ngay nhận ưu đãi</Text>
            <Text style={styles.bannerSubtitle}>Giảm giá 20% cho gói VIP lần đầu</Text>
          </View>
        </View>

        <Text style={styles.sectionTitle}>CÁC GÓI NẠP PHỔ BIẾN</Text>
        {packages.map(pkg => renderPackage(pkg))}

        {isDevMode && (
          <>
            <Text style={[styles.sectionTitle, { color: '#ff00ff', marginTop: 30 }]}>DEV TOOLS (TEST ONLY)</Text>
            {devPackages.map(pkg => renderPackage(pkg, true))}
          </>
        )}

        <View style={styles.footerInfo}>
          <View style={styles.infoItem}>
            <ShieldCheck color="#4CAF50" size={16} />
            <Text style={styles.infoText}>Giao dịch bảo mật 100%</Text>
          </View>
          <View style={styles.infoItem}>
            <AlertCircle color="#666" size={16} />
            <Text style={styles.infoText}>Hỗ trợ 24/7: 1900 xxxx</Text>
          </View>
        </View>
      </ScrollView>

      {/* Payment Method Modal */}
      <Modal visible={showPaymentModal} transparent animationType="slide">
        <View style={styles.modalOverlay}>
          <View style={styles.modalContent}>
            <Text style={styles.modalTitle}>Chọn phương thức thanh toán</Text>
            <Text style={styles.modalSub}>Gói: {selectedPackage?.name}</Text>
            
            <TouchableOpacity 
              style={styles.methodBtn} 
              onPress={() => initiatePayment(selectedPackage.id, 'vnpay')}
            >
              <CreditCard color="#005BAA" size={24} />
              <Text style={styles.methodText}>VNPay Sandbox</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={[styles.methodBtn, { marginTop: 15 }]} 
              onPress={() => initiatePayment(selectedPackage.id, 'momo')}
            >
              <Wallet color="#A50064" size={24} />
              <Text style={styles.methodText}>MoMo Sandbox</Text>
            </TouchableOpacity>

            <TouchableOpacity 
              style={styles.closeBtn} 
              onPress={() => setShowPaymentModal(false)}
            >
              <Text style={styles.closeBtnText}>Hủy bỏ</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Modal>

      {/* WebView for Payment */}
      <Modal visible={!!paymentUrl} animationType="fade">
        <SafeAreaView style={{ flex: 1, backgroundColor: '#fff' }}>
          <View style={styles.webviewHeader}>
            <TouchableOpacity onPress={() => setPaymentUrl(null)}>
              <Text style={styles.webviewClose}>Đóng</Text>
            </TouchableOpacity>
            <Text style={styles.webviewTitle}>Thanh toán An toàn</Text>
            <View style={{ width: 50 }} />
          </View>
          <WebView 
            source={{ uri: paymentUrl }} 
            onNavigationStateChange={(navState) => {
              if (navState.url.includes('vnpay_return') || navState.url.includes('momo_return')) {
                // Delay to show the result page in webview for a bit
                setTimeout(() => {
                  setPaymentUrl(null);
                  fetchBalance();
                }, 3000);
              }
            }}
          />
        </SafeAreaView>
      </Modal>

      {loading && (
        <View style={styles.loadingOverlay}>
          <ActivityIndicator size="large" color="#ffd700" />
        </View>
      )}
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#0f0f15',
  },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'center',
    padding: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#1e1e2d',
  },
  headerTitle: {
    fontSize: 22,
    fontWeight: '900',
    color: '#fff',
    letterSpacing: 2,
  },
  balanceContainer: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#1e1e2d',
    paddingHorizontal: 12,
    paddingVertical: 6,
    borderRadius: 20,
  },
  balanceText: {
    color: '#ffd700',
    fontWeight: 'bold',
    marginLeft: 8,
    fontSize: 16,
  },
  scrollContent: {
    padding: 20,
  },
  banner: {
    backgroundColor: '#1e1e2d',
    borderRadius: 15,
    padding: 20,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 30,
    borderLeftWidth: 4,
    borderLeftColor: '#ffd700',
  },
  bannerTitle: {
    color: '#fff',
    fontSize: 18,
    fontWeight: 'bold',
    marginLeft: 15,
  },
  bannerSubtitle: {
    color: '#888',
    fontSize: 14,
    marginLeft: 15,
  },
  sectionTitle: {
    color: '#666',
    fontSize: 12,
    fontWeight: 'bold',
    letterSpacing: 1,
    marginBottom: 15,
  },
  packageCard: {
    backgroundColor: '#1e1e2d',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 12,
    borderWidth: 1,
    borderColor: '#2a2a3d',
  },
  devCard: {
    borderColor: '#ff00ff',
    borderStyle: 'dashed',
  },
  packageIcon: {
    width: 50,
    height: 50,
    borderRadius: 25,
    backgroundColor: '#161621',
    justifyContent: 'center',
    alignItems: 'center',
  },
  packageInfo: {
    flex: 1,
    marginLeft: 15,
  },
  packageName: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
  },
  packageDesc: {
    color: '#888',
    fontSize: 12,
    marginTop: 2,
  },
  packagePrice: {
    flexDirection: 'row',
    alignItems: 'center',
  },
  priceText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
    marginRight: 10,
  },
  modalOverlay: {
    flex: 1,
    backgroundColor: 'rgba(0,0,0,0.8)',
    justifyContent: 'flex-end',
  },
  modalContent: {
    backgroundColor: '#1e1e2d',
    borderTopLeftRadius: 25,
    borderTopRightRadius: 25,
    padding: 30,
    paddingBottom: 50,
  },
  modalTitle: {
    color: '#fff',
    fontSize: 20,
    fontWeight: 'bold',
    textAlign: 'center',
  },
  modalSub: {
    color: '#888',
    textAlign: 'center',
    marginTop: 5,
    marginBottom: 25,
  },
  methodBtn: {
    flexDirection: 'row',
    alignItems: 'center',
    backgroundColor: '#2a2a3d',
    padding: 20,
    borderRadius: 15,
  },
  methodText: {
    color: '#fff',
    fontSize: 16,
    fontWeight: 'bold',
    marginLeft: 15,
  },
  closeBtn: {
    marginTop: 25,
    alignItems: 'center',
  },
  closeBtnText: {
    color: '#666',
    fontSize: 16,
  },
  webviewHeader: {
    height: 60,
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 20,
    borderBottomWidth: 1,
    borderBottomColor: '#eee',
  },
  webviewClose: {
    color: '#007AFF',
    fontSize: 16,
  },
  webviewTitle: {
    fontWeight: 'bold',
    fontSize: 16,
  },
  loadingOverlay: {
    ...StyleSheet.absoluteFillObject,
    backgroundColor: 'rgba(0,0,0,0.5)',
    justifyContent: 'center',
    alignItems: 'center',
  },
  footerInfo: {
    marginTop: 40,
    alignItems: 'center',
  },
  infoItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 10,
  },
  infoText: {
    color: '#444',
    fontSize: 12,
    marginLeft: 8,
  }
});

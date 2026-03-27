import { BrowserRouter } from 'react-router-dom';
import { Toaster } from 'react-hot-toast';
import { AuthProvider } from './context/AuthContext';
import AppRoutes from './routes/AppRoutes';
import Chatbot from './components/common/Chatbot';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
        <Toaster
          position="top-right"
          toastOptions={{
            duration: 4000,
            error: {
              duration: 6000,
              style: {
                background: '#FEF2F2',
                color: '#991B1B',
                border: '1px solid #FCA5A5',
              },
            },
            style: {
              borderRadius: '10px',
              background: '#333',
              color: '#fff',
              fontSize: '14px',
            },
            success: { style: { background: '#10b981' } },
            error:   { style: { background: '#ef4444' } },
          }}
        />
        <Chatbot />
      </AuthProvider>
    </BrowserRouter>
  );
}

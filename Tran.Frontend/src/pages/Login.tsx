import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import apiClient from '../services/api';

export default function Login() {
  const navigate = useNavigate();
  const [userId, setUserId] = useState('admin');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const res = await apiClient.post('/auth/login', { userId, password: '' });
      const data = res.data.data;
      localStorage.setItem('authToken', data.token);
      localStorage.setItem('user', JSON.stringify(data));
      navigate('/');
    } catch (err: any) {
      setError(err.response?.data?.message || '로그인에 실패했습니다.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-[#2E4A7A] via-[#3B5998] to-[#4A6FA5] flex items-center justify-center p-4">
      <div className="bg-white rounded-2xl shadow-2xl w-full max-w-[380px] p-8 sm:p-10">
        <div className="flex items-center justify-center gap-3 mb-8">
          <div className="w-10 h-10 bg-gradient-to-br from-[#2E4A7A] to-[#4A6FA5] rounded-lg flex items-center justify-center text-white text-lg font-extrabold">
            T
          </div>
          <div>
            <div className="text-xl font-bold text-gray-900 tracking-tight">Tran ERP</div>
            <div className="text-[11px] text-gray-400">Enterprise Resource Planning</div>
          </div>
        </div>

        <form onSubmit={handleLogin}>
          <div className="mb-5">
            <label className="form-label">사용자 ID</label>
            <input
              type="text"
              value={userId}
              onChange={e => setUserId(e.target.value)}
              placeholder="사용자 ID 입력"
              className="form-input !py-2.5"
              autoFocus
            />
          </div>

          {error && (
            <div className="mb-4 px-3.5 py-2.5 rounded-lg bg-red-50 text-red-600 text-[13px] font-medium">
              {error}
            </div>
          )}

          <button
            type="submit"
            disabled={loading || !userId.trim()}
            className="w-full py-2.5 rounded-lg bg-gradient-to-r from-[#2E4A7A] to-[#4A6FA5] text-white text-[13px] font-semibold shadow-lg hover:shadow-xl transition-all disabled:opacity-50"
          >
            {loading ? '로그인 중...' : '로그인'}
          </button>
        </form>

        <div className="mt-6 text-center text-[11px] text-gray-400">
          기본 계정: admin
        </div>
      </div>
    </div>
  );
}

import React, { useState } from 'react';

interface ShareImageModalProps {
  imageId: string;
  imageTitle: string;
  isOpen: boolean;
  onClose: () => void;
  onShareSuccess?: () => void;
}

const ShareImageModal: React.FC<ShareImageModalProps> = ({
  imageId,
  imageTitle,
  isOpen,
  onClose,
  onShareSuccess
}) => {
  const [targetUserId, setTargetUserId] = useState('');
  const [message, setMessage] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!targetUserId.trim()) {
      setError('공유할 사용자의 ID를 입력해주세요.');
      return;
    }

    setIsSubmitting(true);
    setError('');

    try {
      const token = localStorage.getItem('accessToken');
      const formData = new FormData();
      formData.append('imageId', imageId);
      formData.append('targetUserId', targetUserId.trim());
      if (message.trim()) {
        formData.append('message', message.trim());
      }

      const response = await fetch('http://localhost:5125/api/share/request', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${token}`
        },
        body: formData
      });

      const result = await response.json();

      if (response.ok && result.success) {
        // 성공 시 모달 닫기 및 초기화
        setTargetUserId('');
        setMessage('');
        onClose();
        
        if (onShareSuccess) {
          onShareSuccess();
        }
        
        // TODO: 성공 알림 표시 (토스트 등)
        alert('공유 요청이 성공적으로 전송되었습니다.');
      } else {
        setError(result.message || '공유 요청 전송에 실패했습니다.');
      }
    } catch (err: any) {
      setError('네트워크 오류가 발생했습니다.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClose = () => {
    if (!isSubmitting) {
      setTargetUserId('');
      setMessage('');
      setError('');
      onClose();
    }
  };

  if (!isOpen) return null;

  return (
    <div 
      style={{
        position: 'fixed',
        top: 0,
        left: 0,
        right: 0,
        bottom: 0,
        backgroundColor: 'rgba(0, 0, 0, 0.5)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
        padding: '1rem'
      }}
      onClick={handleClose}
    >
      <div 
        style={{ 
          backgroundColor: 'white',
          borderRadius: '8px',
          width: '100%',
          maxWidth: '500px',
          maxHeight: '90vh',
          overflow: 'auto'
        }}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div style={{ 
          padding: '1.5rem 1.5rem 1rem 1.5rem',
          borderBottom: '1px solid #e5e7eb'
        }}>
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <h3 style={{ 
              margin: 0, 
              fontSize: '1.25rem', 
              fontWeight: '600', 
              color: '#1f2937' 
            }}>
              이미지 공유하기
            </h3>
            <button
              onClick={handleClose}
              disabled={isSubmitting}
              style={{
                background: 'none',
                border: 'none',
                fontSize: '1.5rem',
                cursor: isSubmitting ? 'not-allowed' : 'pointer',
                color: '#6b7280',
                padding: '0.25rem'
              }}
            >
              ×
            </button>
          </div>
          <p style={{ 
            margin: '0.5rem 0 0 0', 
            fontSize: '0.875rem', 
            color: '#6b7280' 
          }}>
            "{imageTitle}"을(를) 다른 사용자와 공유합니다
          </p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} style={{ padding: '1.5rem' }}>
          {/* Target User ID */}
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '0.875rem', 
              fontWeight: '500', 
              color: '#374151' 
            }}>
              공유할 사용자 ID *
            </label>
            <input
              type="text"
              className="input"
              value={targetUserId}
              onChange={(e) => setTargetUserId(e.target.value)}
              placeholder="예: 22222222-2222-2222-2222-222222222222"
              disabled={isSubmitting}
              style={{ width: '100%' }}
              required
            />
            <div style={{ 
              fontSize: '0.75rem', 
              color: '#9ca3af',
              marginTop: '0.25rem'
            }}>
              💡 테스트용으로 "22222222-2222-2222-2222-222222222222" 사용 가능
            </div>
          </div>

          {/* Message */}
          <div style={{ marginBottom: '1rem' }}>
            <label style={{ 
              display: 'block', 
              marginBottom: '0.5rem', 
              fontSize: '0.875rem', 
              fontWeight: '500', 
              color: '#374151' 
            }}>
              메시지 (선택사항)
            </label>
            <textarea
              className="input"
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              placeholder="공유와 함께 보낼 메시지를 입력하세요..."
              disabled={isSubmitting}
              rows={3}
              style={{ width: '100%', resize: 'vertical' }}
              maxLength={500}
            />
            <div style={{ 
              fontSize: '0.75rem', 
              color: '#9ca3af', 
              textAlign: 'right',
              marginTop: '0.25rem'
            }}>
              {message.length}/500
            </div>
          </div>

          {/* Error Message */}
          {error && (
            <div style={{ 
              backgroundColor: '#fef2f2',
              border: '1px solid #fecaca',
              color: '#dc2626',
              padding: '0.75rem',
              borderRadius: '6px',
              fontSize: '0.875rem',
              marginBottom: '1rem'
            }}>
              {error}
            </div>
          )}

          {/* Buttons */}
          <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
            <button
              type="button"
              onClick={handleClose}
              disabled={isSubmitting}
              className="btn"
              style={{ 
                padding: '0.75rem 1.5rem',
                backgroundColor: 'white',
                color: '#374151',
                border: '1px solid #d1d5db'
              }}
            >
              취소
            </button>
            <button
              type="submit"
              disabled={isSubmitting || !targetUserId.trim()}
              className="btn btn-primary"
              style={{ 
                padding: '0.75rem 1.5rem',
                opacity: isSubmitting || !targetUserId.trim() ? 0.5 : 1
              }}
            >
              {isSubmitting ? '전송 중...' : '공유 요청'}
            </button>
          </div>
        </form>

        {/* Info */}
        <div style={{ 
          padding: '1rem 1.5rem',
          backgroundColor: '#f9fafb',
          borderTop: '1px solid #e5e7eb',
          fontSize: '0.75rem',
          color: '#6b7280'
        }}>
          💡 공유 요청을 받은 사용자가 승인하면 이미지를 볼 수 있습니다.
        </div>
      </div>
    </div>
  );
};

export default ShareImageModal;
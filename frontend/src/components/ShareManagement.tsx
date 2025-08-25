import React, { useState, useEffect } from 'react';

interface ShareRequest {
  id: string;
  imageId: string;
  imageFileName: string;
  requesterId: string;
  requesterEmail: string;
  requesterName: string;
  message: string;
  status: string;
  createdAt: string;
  respondedAt?: string;
}

interface ShareManagementProps {
  type: 'received' | 'sent';
}

const ShareManagement: React.FC<ShareManagementProps> = ({ type }) => {
  const [shareRequests, setShareRequests] = useState<ShareRequest[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [processingIds, setProcessingIds] = useState<Set<string>>(new Set());

  const pageSize = 10;

  useEffect(() => {
    loadShareRequests();
  }, [type, currentPage]);

  const loadShareRequests = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('accessToken');
      const endpoint = type === 'received' ? 'received' : 'sent';
      
      const response = await fetch(
        `http://localhost:5125/api/share/${endpoint}?page=${currentPage}&pageSize=${pageSize}`,
        {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        }
      );

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          setShareRequests(result.data.shareRequests);
          setTotalPages(result.data.pagination.totalPages);
        } else {
          setError(result.message || '공유 요청을 불러오는데 실패했습니다.');
        }
      } else {
        setError('공유 요청을 불러오는데 실패했습니다.');
      }
    } catch (err: any) {
      setError('네트워크 오류가 발생했습니다.');
    } finally {
      setLoading(false);
    }
  };

  const handleShareRequest = async (requestId: string, action: 'approve' | 'reject') => {
    setProcessingIds(prev => {
      const newSet = new Set(prev);
      newSet.add(requestId);
      return newSet;
    });
    
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(
        `http://localhost:5125/api/share/${requestId}/${action}`,
        {
          method: 'POST',
          headers: {
            'Authorization': `Bearer ${token}`
          }
        }
      );

      if (response.ok) {
        // 요청 목록 새로고침
        await loadShareRequests();
        // TODO: 성공 알림 추가
      } else {
        const result = await response.json();
        setError(result.message || `${action === 'approve' ? '승인' : '거절'} 처리에 실패했습니다.`);
      }
    } catch (err: any) {
      setError('네트워크 오류가 발생했습니다.');
    } finally {
      setProcessingIds(prev => {
        const newSet = new Set(prev);
        newSet.delete(requestId);
        return newSet;
      });
    }
  };

  const getStatusColor = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending': return '#f59e0b';
      case 'approved': return '#10b981';
      case 'rejected': return '#ef4444';
      default: return '#6b7280';
    }
  };

  const getStatusText = (status: string) => {
    switch (status.toLowerCase()) {
      case 'pending': return '대기중';
      case 'approved': return '승인됨';
      case 'rejected': return '거절됨';
      default: return status;
    }
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('ko-KR');
  };

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>
          공유 요청을 불러오는 중...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#dc2626', marginBottom: '1rem' }}>
          {error}
        </div>
        <button onClick={loadShareRequests} className="btn btn-primary">
          다시 시도
        </button>
      </div>
    );
  }

  return (
    <div style={{ padding: '2rem' }}>
      {/* Header */}
      <div className="card" style={{ marginBottom: '2rem' }}>
        <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937', margin: 0 }}>
          {type === 'received' ? '받은 공유 요청' : '보낸 공유 요청'} ({shareRequests.length}개)
        </h2>
      </div>

      {/* Share Requests List */}
      {shareRequests.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '3rem' }}>
          <div style={{ fontSize: '3rem', marginBottom: '1rem', opacity: 0.5 }}>📤</div>
          <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>
            {type === 'received' ? '받은 공유 요청이 없습니다.' : '보낸 공유 요청이 없습니다.'}
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
          {shareRequests.map(request => (
            <div key={request.id} className="card" style={{ padding: '1.5rem' }}>
              <div style={{ 
                display: 'grid', 
                gridTemplateColumns: 'auto 1fr auto', 
                gap: '1rem', 
                alignItems: 'center' 
              }}>
                {/* Status Badge */}
                <div style={{
                  backgroundColor: getStatusColor(request.status),
                  color: 'white',
                  padding: '0.25rem 0.75rem',
                  borderRadius: '12px',
                  fontSize: '0.75rem',
                  fontWeight: '500',
                  textAlign: 'center',
                  minWidth: '60px'
                }}>
                  {getStatusText(request.status)}
                </div>

                {/* Request Info */}
                <div style={{ minWidth: 0 }}>
                  <h4 style={{ 
                    margin: 0, 
                    marginBottom: '0.5rem', 
                    fontSize: '1rem', 
                    fontWeight: '500',
                    color: '#1f2937'
                  }}>
                    파일: {request.imageFileName}
                  </h4>
                  <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' }}>
                    {type === 'received' ? (
                      <>요청자: {request.requesterName || request.requesterEmail}</>
                    ) : (
                      <>수신자: {request.requesterEmail}</>
                    )}
                  </div>
                  {request.message && (
                    <div style={{ 
                      fontSize: '0.875rem', 
                      color: '#374151', 
                      backgroundColor: '#f9fafb',
                      padding: '0.5rem',
                      borderRadius: '4px',
                      marginBottom: '0.5rem',
                      fontStyle: 'italic'
                    }}>
                      "{request.message}"
                    </div>
                  )}
                  <div style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                    요청일: {formatDate(request.createdAt)}
                    {request.respondedAt && (
                      <> | 처리일: {formatDate(request.respondedAt)}</>
                    )}
                  </div>
                </div>

                {/* Action Buttons */}
                {type === 'received' && request.status.toLowerCase() === 'pending' && (
                  <div style={{ display: 'flex', gap: '0.5rem' }}>
                    <button
                      onClick={() => handleShareRequest(request.id, 'approve')}
                      disabled={processingIds.has(request.id)}
                      className="btn"
                      style={{
                        backgroundColor: '#10b981',
                        color: 'white',
                        padding: '0.5rem 1rem',
                        fontSize: '0.875rem',
                        opacity: processingIds.has(request.id) ? 0.5 : 1
                      }}
                    >
                      {processingIds.has(request.id) ? '처리중...' : '승인'}
                    </button>
                    <button
                      onClick={() => handleShareRequest(request.id, 'reject')}
                      disabled={processingIds.has(request.id)}
                      className="btn"
                      style={{
                        backgroundColor: '#ef4444',
                        color: 'white',
                        padding: '0.5rem 1rem',
                        fontSize: '0.875rem',
                        opacity: processingIds.has(request.id) ? 0.5 : 1
                      }}
                    >
                      {processingIds.has(request.id) ? '처리중...' : '거절'}
                    </button>
                  </div>
                )}
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem', marginTop: '2rem' }}>
          <button
            onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
            disabled={currentPage === 1}
            className="btn"
            style={{ 
              padding: '0.5rem 1rem',
              backgroundColor: currentPage === 1 ? '#f3f4f6' : 'white',
              color: currentPage === 1 ? '#9ca3af' : '#374151'
            }}
          >
            이전
          </button>
          
          {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
            const startPage = Math.max(1, currentPage - 2);
            const page = startPage + i;
            if (page > totalPages) return null;
            
            return (
              <button
                key={page}
                onClick={() => setCurrentPage(page)}
                className="btn"
                style={{
                  padding: '0.5rem 1rem',
                  backgroundColor: currentPage === page ? '#3b82f6' : 'white',
                  color: currentPage === page ? 'white' : '#374151'
                }}
              >
                {page}
              </button>
            );
          })}
          
          <button
            onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
            disabled={currentPage === totalPages}
            className="btn"
            style={{ 
              padding: '0.5rem 1rem',
              backgroundColor: currentPage === totalPages ? '#f3f4f6' : 'white',
              color: currentPage === totalPages ? '#9ca3af' : '#374151'
            }}
          >
            다음
          </button>
        </div>
      )}
    </div>
  );
};

export default ShareManagement;
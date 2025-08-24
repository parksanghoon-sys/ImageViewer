import React, { useState } from 'react';

interface BlurredImageProps {
  src: string;
  alt: string;
  blurIntensity?: number; // 0-100
  className?: string;
  previewSize?: number; // pixels
  style?: React.CSSProperties; // ✅ 추가
  onClick?: () => void;        // 필요시
}

const BlurredImage: React.FC<BlurredImageProps> = ({ 
  src, 
  alt, 
  blurIntensity = 50, 
  className = '',
  previewSize = 200 
}) => {
  const [isHovered, setIsHovered] = useState(false);
  const [isClicked, setIsClicked] = useState(false);

  // Convert blur intensity (0-100) to CSS blur value (0-20px)
  const blurValue = (blurIntensity / 100) * 20;

  const getBlurStyle = () => {
    if (isClicked) {
      return { filter: 'blur(0px)', transition: 'filter 0.3s ease' };
    }
    if (isHovered) {
      return { filter: `blur(${blurValue / 2}px)`, transition: 'filter 0.3s ease' };
    }
    return { filter: `blur(${blurValue}px)`, transition: 'filter 0.3s ease' };
  };

  const handleClick = () => {
    setIsClicked(!isClicked);
  };

  return (
    <div 
      className={`relative cursor-pointer ${className}`}
      style={{ width: previewSize, height: previewSize }}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      onClick={handleClick}
    >
      <img
        src={src}
        alt={alt}
        className="w-full h-full object-cover rounded-lg shadow-md"
        style={getBlurStyle()}
      />
      
      {/* Overlay with blur info */}
      {!isClicked && (
        <div className="absolute inset-0 flex items-center justify-center bg-black bg-opacity-20 rounded-lg opacity-0 hover:opacity-100 transition-opacity duration-300">
          <div className="text-white text-xs text-center p-2">
            <div>클릭하여 선명하게</div>
            <div className="mt-1">블러: {blurIntensity}%</div>
          </div>
        </div>
      )}

      {/* Click to restore blur button when unblurred */}
      {isClicked && (
        <div className="absolute top-2 right-2">
          <button
            onClick={(e) => {
              e.stopPropagation();
              setIsClicked(false);
            }}
            className="bg-black bg-opacity-50 text-white rounded-full w-6 h-6 flex items-center justify-center text-xs hover:bg-opacity-70"
          >
            ×
          </button>
        </div>
      )}
    </div>
  );
};

export default BlurredImage;
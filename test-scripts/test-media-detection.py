#!/usr/bin/env python3
"""
Test script for media file type detection.
Tests both content-based and HTTP header-based detection.
"""

import sys
import os

# Add parent directory to path to import the upload_media module
sys.path.insert(0, os.path.join(os.path.dirname(__file__), '..', '.github', 'scripts'))

from upload_media import detect_file_extension_from_content, detect_extension_from_content_type

def test_video_detection():
    """Test video file detection from content."""
    print("Testing video file detection...")
    
    # MP4 file signature (ftyp box with isom)
    mp4_content = b'\x00\x00\x00\x20ftypisom\x00\x00\x02\x00isomiso2avc1mp41'
    result = detect_file_extension_from_content(mp4_content)
    assert result == '.mp4', f"Expected .mp4, got {result}"
    print("  ✅ MP4 detection: PASSED")
    
    # Another MP4 variant (mp42)
    mp4_content2 = b'\x00\x00\x00\x18ftypmp42\x00\x00\x00\x00isommp42'
    result = detect_file_extension_from_content(mp4_content2)
    assert result == '.mp4', f"Expected .mp4, got {result}"
    print("  ✅ MP4 (variant) detection: PASSED")
    
    # WebM/MKV
    mkv_content = b'\x1a\x45\xdf\xa3\x93B\x82\x88matroska'
    result = detect_file_extension_from_content(mkv_content)
    assert result == '.mkv', f"Expected .mkv, got {result}"
    print("  ✅ MKV detection: PASSED")
    
    # AVI
    avi_content = b'RIFF\x00\x00\x00\x00AVI LIST'
    result = detect_file_extension_from_content(avi_content)
    assert result == '.avi', f"Expected .avi, got {result}"
    print("  ✅ AVI detection: PASSED")


def test_image_detection():
    """Test image file detection from content."""
    print("\nTesting image file detection...")
    
    # JPEG
    jpeg_content = b'\xff\xd8\xff\xe0\x00\x10JFIF'
    result = detect_file_extension_from_content(jpeg_content)
    assert result == '.jpg', f"Expected .jpg, got {result}"
    print("  ✅ JPEG detection: PASSED")
    
    # PNG
    png_content = b'\x89PNG\r\n\x1a\n\x00\x00\x00\rIHDR'
    result = detect_file_extension_from_content(png_content)
    assert result == '.png', f"Expected .png, got {result}"
    print("  ✅ PNG detection: PASSED")
    
    # GIF
    gif_content = b'GIF89a\x01\x00\x01\x00\x80\x00\x00'
    result = detect_file_extension_from_content(gif_content)
    assert result == '.gif', f"Expected .gif, got {result}"
    print("  ✅ GIF detection: PASSED")
    
    # WebP
    webp_content = b'RIFF\x00\x00\x00\x00WEBPVP8 '
    result = detect_file_extension_from_content(webp_content)
    assert result == '.webp', f"Expected .webp, got {result}"
    print("  ✅ WebP detection: PASSED")


def test_audio_detection():
    """Test audio file detection from content."""
    print("\nTesting audio file detection...")
    
    # MP3 with ID3 tag
    mp3_content = b'ID3\x03\x00\x00\x00\x00\x00\x00'
    result = detect_file_extension_from_content(mp3_content)
    assert result == '.mp3', f"Expected .mp3, got {result}"
    print("  ✅ MP3 (ID3) detection: PASSED")
    
    # MP3 with frame sync
    mp3_content2 = b'\xff\xfb\x90\x00\x00\x00\x00\x00'
    result = detect_file_extension_from_content(mp3_content2)
    assert result == '.mp3', f"Expected .mp3, got {result}"
    print("  ✅ MP3 (frame) detection: PASSED")
    
    # WAV
    wav_content = b'RIFF\x00\x00\x00\x00WAVEfmt '
    result = detect_file_extension_from_content(wav_content)
    assert result == '.wav', f"Expected .wav, got {result}"
    print("  ✅ WAV detection: PASSED")
    
    # OGG
    ogg_content = b'OggS\x00\x02\x00\x00\x00\x00'
    result = detect_file_extension_from_content(ogg_content)
    assert result == '.ogg', f"Expected .ogg, got {result}"
    print("  ✅ OGG detection: PASSED")


def test_content_type_detection():
    """Test detection from HTTP Content-Type headers."""
    print("\nTesting Content-Type header detection...")
    
    # Video types
    result = detect_extension_from_content_type('video/mp4')
    assert result == '.mp4', f"Expected .mp4, got {result}"
    print("  ✅ video/mp4: PASSED")
    
    result = detect_extension_from_content_type('video/quicktime')
    assert result == '.mov', f"Expected .mov, got {result}"
    print("  ✅ video/quicktime: PASSED")
    
    result = detect_extension_from_content_type('video/webm')
    assert result == '.webm', f"Expected .webm, got {result}"
    print("  ✅ video/webm: PASSED")
    
    # Image types
    result = detect_extension_from_content_type('image/jpeg')
    assert result == '.jpg', f"Expected .jpg, got {result}"
    print("  ✅ image/jpeg: PASSED")
    
    result = detect_extension_from_content_type('image/png')
    assert result == '.png', f"Expected .png, got {result}"
    print("  ✅ image/png: PASSED")
    
    # Audio types
    result = detect_extension_from_content_type('audio/mpeg')
    assert result == '.mp3', f"Expected .mp3, got {result}"
    print("  ✅ audio/mpeg: PASSED")
    
    result = detect_extension_from_content_type('audio/wav')
    assert result == '.wav', f"Expected .wav, got {result}"
    print("  ✅ audio/wav: PASSED")
    
    # Content-Type with parameters
    result = detect_extension_from_content_type('video/mp4; codecs="avc1.42E01E"')
    assert result == '.mp4', f"Expected .mp4, got {result}"
    print("  ✅ video/mp4 with parameters: PASSED")


def test_unknown_detection():
    """Test detection of unknown file types."""
    print("\nTesting unknown file type handling...")
    
    # Random bytes
    random_content = b'\x00\x01\x02\x03\x04\x05\x06\x07\x08\x09\x0a\x0b'
    result = detect_file_extension_from_content(random_content)
    assert result is None, f"Expected None, got {result}"
    print("  ✅ Unknown content: PASSED")
    
    # Unknown content type
    result = detect_extension_from_content_type('application/octet-stream')
    assert result is None, f"Expected None, got {result}"
    print("  ✅ Unknown Content-Type: PASSED")
    
    # Empty content
    result = detect_file_extension_from_content(b'')
    assert result is None, f"Expected None, got {result}"
    print("  ✅ Empty content: PASSED")


if __name__ == '__main__':
    print("=" * 60)
    print("Media File Type Detection Tests")
    print("=" * 60)
    
    try:
        test_video_detection()
        test_image_detection()
        test_audio_detection()
        test_content_type_detection()
        test_unknown_detection()
        
        print("\n" + "=" * 60)
        print("✅ All tests PASSED!")
        print("=" * 60)
        sys.exit(0)
    except AssertionError as e:
        print(f"\n❌ Test FAILED: {e}")
        sys.exit(1)
    except Exception as e:
        print(f"\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)

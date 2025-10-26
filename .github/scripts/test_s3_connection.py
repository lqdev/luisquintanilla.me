#!/usr/bin/env python3
"""
Test S3 Connection Script

This script tests the S3/Linode Object Storage connection configuration
to diagnose connection issues. It does NOT expose credentials in logs.

Usage: python test_s3_connection.py
"""

import os
import sys
import boto3
from botocore.config import Config
from urllib.parse import urlparse


def mask_credential(value, show_chars=4):
    """Mask credentials for safe logging."""
    if not value or len(value) <= show_chars:
        return "***"
    return value[:show_chars] + ("*" * (len(value) - show_chars))


def test_s3_connection():
    """Test S3 connection with various configurations."""
    
    # Get S3 configuration from environment
    access_key = os.environ.get('LINODE_STORAGE_ACCESS_KEY_ID')
    secret_key = os.environ.get('LINODE_STORAGE_SECRET_ACCESS_KEY')
    endpoint_url = os.environ.get('LINODE_STORAGE_ENDPOINT_URL')
    bucket_name = os.environ.get('LINODE_STORAGE_BUCKET_NAME')
    custom_domain = os.environ.get('LINODE_STORAGE_CUSTOM_DOMAIN')
    
    print("=" * 70)
    print("🔧 S3 Connection Test - Configuration Check")
    print("=" * 70)
    print(f"Access Key ID: {'✅ Set' if access_key else '❌ Missing'} ({mask_credential(access_key) if access_key else 'N/A'})")
    print(f"Secret Access Key: {'✅ Set' if secret_key else '❌ Missing'} ({mask_credential(secret_key) if secret_key else 'N/A'})")
    print(f"Endpoint URL: {'✅ Set' if endpoint_url else '❌ Missing'} ({endpoint_url if endpoint_url else 'N/A'})")
    print(f"Bucket Name: {'✅ Set' if bucket_name else '❌ Missing'} ({bucket_name if bucket_name else 'N/A'})")
    print(f"Custom Domain: {'✅ Set' if custom_domain else '⚠️  Optional'} ({custom_domain if custom_domain else 'N/A'})")
    print("=" * 70)
    
    if not all([access_key, secret_key, endpoint_url, bucket_name]):
        print("\n❌ Missing required environment variables")
        print("Required:")
        print("  - LINODE_STORAGE_ACCESS_KEY_ID")
        print("  - LINODE_STORAGE_SECRET_ACCESS_KEY")
        print("  - LINODE_STORAGE_ENDPOINT_URL")
        print("  - LINODE_STORAGE_BUCKET_NAME")
        return False
    
    # Extract region from endpoint URL
    try:
        parsed_endpoint = urlparse(endpoint_url)
        if not parsed_endpoint.hostname:
            print("\n❌ Invalid endpoint URL - no hostname found")
            return False
            
        region = parsed_endpoint.hostname.split('.')[0]
        print(f"\n🌍 Detected region from endpoint: {region}")
        
    except Exception as e:
        print(f"\n❌ Error parsing endpoint URL: {e}")
        return False
    
    # Test configurations
    test_configs = [
        {
            "name": "Current Configuration (with timeouts)",
            "config": Config(
                signature_version='s3v4',
                s3={'addressing_style': 'virtual'},
                connect_timeout=60,
                read_timeout=60,
                retries={'max_attempts': 3, 'mode': 'standard'}
            )
        },
        {
            "name": "Original Configuration (without timeouts)",
            "config": Config(
                signature_version='s3v4',
                s3={'addressing_style': 'virtual'}
            )
        },
        {
            "name": "Path-style Addressing",
            "config": Config(
                signature_version='s3v4',
                s3={'addressing_style': 'path'},
                connect_timeout=60,
                read_timeout=60,
                retries={'max_attempts': 3, 'mode': 'standard'}
            )
        }
    ]
    
    success = False
    
    for test_config in test_configs:
        print(f"\n{'=' * 70}")
        print(f"Testing: {test_config['name']}")
        print("=" * 70)
        
        try:
            # Initialize S3 client with test configuration
            s3_client = boto3.client(
                's3',
                aws_access_key_id=access_key,
                aws_secret_access_key=secret_key,
                endpoint_url=endpoint_url,
                region_name=region,
                config=test_config['config']
            )
            
            # Test 1: List buckets
            print("\n📋 Test 1: Listing buckets...")
            response = s3_client.list_buckets()
            bucket_names = [b['Name'] for b in response['Buckets']]
            print(f"✅ Successfully connected to S3!")
            print(f"📦 Found {len(bucket_names)} bucket(s): {', '.join(bucket_names)}")
            
            # Test 2: Access specific bucket
            print(f"\n📂 Test 2: Accessing bucket '{bucket_name}'...")
            if bucket_name not in bucket_names:
                print(f"⚠️  Warning: Bucket '{bucket_name}' not found in available buckets")
                print(f"   Available buckets: {', '.join(bucket_names)}")
            else:
                response = s3_client.list_objects_v2(Bucket=bucket_name, MaxKeys=5)
                object_count = response.get('KeyCount', 0)
                print(f"✅ Successfully accessed bucket '{bucket_name}'!")
                print(f"📁 Found {object_count} object(s) in bucket (showing max 5)")
                
                if 'Contents' in response and response['Contents']:
                    print("\n📄 Sample objects:")
                    for obj in response['Contents'][:5]:
                        size_kb = obj['Size'] / 1024
                        print(f"  - {obj['Key']} ({size_kb:.2f} KB)")
            
            # Test 3: Upload test
            print(f"\n📤 Test 3: Testing upload capability...")
            test_key = "test/connection_test.txt"
            test_content = b"S3 connection test successful!"
            
            s3_client.put_object(
                Bucket=bucket_name,
                Key=test_key,
                Body=test_content,
                ACL='public-read'
            )
            print(f"✅ Successfully uploaded test object: {test_key}")
            
            # Test 4: Generate URL
            if custom_domain:
                test_url = f"{custom_domain.rstrip('/')}/{test_key}"
            else:
                test_url = f"https://{bucket_name}.{parsed_endpoint.hostname}/{test_key}"
            
            print(f"🔗 Test object URL: {test_url}")
            
            # Clean up test object
            print(f"\n🧹 Cleaning up test object...")
            s3_client.delete_object(Bucket=bucket_name, Key=test_key)
            print(f"✅ Test object deleted")
            
            print(f"\n{'=' * 70}")
            print(f"✅ SUCCESS: Configuration '{test_config['name']}' works!")
            print("=" * 70)
            success = True
            break  # Stop testing once we find a working configuration
            
        except Exception as e:
            print(f"\n❌ FAILED with configuration '{test_config['name']}'")
            print(f"   Error: {type(e).__name__}: {str(e)}")
            
            # Don't show full traceback to avoid credential exposure
            if "--verbose" in sys.argv:
                import traceback
                print("\n📋 Full traceback (verbose mode):")
                traceback.print_exc()
    
    print(f"\n{'=' * 70}")
    if success:
        print("✅ S3 CONNECTION TEST PASSED")
        print("   The S3 client can successfully connect and upload files.")
    else:
        print("❌ S3 CONNECTION TEST FAILED")
        print("   All tested configurations failed. Check credentials and endpoint.")
    print("=" * 70)
    
    return success


if __name__ == '__main__':
    try:
        success = test_s3_connection()
        sys.exit(0 if success else 1)
    except KeyboardInterrupt:
        print("\n\n⚠️  Test interrupted by user")
        sys.exit(1)
    except Exception as e:
        print(f"\n\n❌ Unexpected error: {type(e).__name__}: {str(e)}")
        sys.exit(1)

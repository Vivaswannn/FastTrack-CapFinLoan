import json
import os

def insert_otp_route(filepath):
    with open(filepath, 'r') as f:
        data = json.load(f)
        
    # Check if route already exists
    for r in data.get('Routes', []):
        if r.get('UpstreamPathTemplate') == '/gateway/auth/verify-otp':
            return
            
    new_route = {
      "UpstreamPathTemplate": "/gateway/auth/verify-otp",
      "UpstreamHttpMethod": ["POST"],
      "DownstreamPathTemplate": "/api/auth/verify-otp",
      "DownstreamScheme": "http",
      "DownstreamHostAndPorts": [
        {
          "Host": "localhost",
          "Port": 5001
        }
      ],
      "QoSOptions": {
        "ExceptionsAllowedBeforeBreaking": 3,
        "DurationOfBreak": 5000,
        "TimeoutValue": 10000
      }
    }
    
    data.setdefault('Routes', []).insert(2, new_route)
    with open(filepath, 'w') as f:
        json.dump(data, f, indent=2)

base_path = r'D:\CapFinLoan\CapFinLoan\Backend\ApiGateway\CapFinLoan.Gateway'
insert_otp_route(os.path.join(base_path, 'ocelot.json'))
insert_otp_route(os.path.join(base_path, 'ocelot.Production.json'))
print("Routes added safely.")

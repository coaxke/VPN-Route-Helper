# VPN-Route-Helper
A Simple .NET console app can set routes based on a config file during dial-up of a VPN – The workflow is as follows:

 1. User dials VPN endpoint
 2. Once VPN Establishes the VPNRouteHelper tool is invoked which:
	 A. Checks if there is a config file on a web-server that is accessible on the one route published
	 B. If the config on the server is newer we replace the existing config with the new one
	 C. We then check for the presence of active PPP dial-up adapters on the computer and grab the tunnels IP address
	 D. Check if that tunnel IP address fits between a set of pre-determined ranges
	 E. If the tunnel fits inside a range we loop through a list of IP ranges we wish to set routes for and then assign a default gateway based on the tunnels IP address
 3. Displays a message of the day (if enabled in config)
 4. Done!

###Check out a blog post about this: http://www.resdevops.com/2016/02/02/vpn-route-helper-tool/

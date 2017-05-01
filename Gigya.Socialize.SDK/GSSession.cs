/*
 * Copyright (C) 2011 Gigya, Inc.
 * Version 2.15.7
 */



using System;
namespace Gigya.Socialize.SDK
{

    /**
     * Wraps a GS session returned from the OAuth2 version of the login process
     * @author Raviv Pavel
     *
     */
    public class GSSession 
    {
	    private string secret;
	    public void setSecret(string secret) { this.secret = secret; }
	    public string getSecret() { return secret; }
    	
	    private string accessToken;
	    public void setAccessToken(string accessToken) { this.accessToken = accessToken; }
	    public string getAccessToken() { return accessToken; }
    		
    	
	    private long expirationTime;
	    public void setExpirationTime(long expirationTime) { this.expirationTime = expirationTime;}
	    public long getExpirationTime() { return expirationTime; }	
    	
	    public GSSession() {}
    	
	    public GSSession(string accessToken, string secret, long expirationSeconds)
	    {
            
		    this.setAccessToken(accessToken);
		    this.setSecret(secret);
            if (expirationSeconds == 0)
                this.setExpirationTime(long.MaxValue);
            else
            {                
                this.setExpirationTime(GSSession.CurrentTimeMillis() + (1000 * expirationSeconds));
            }
	    }

        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

	    public GSSession(GSObject currDictionaryParams)	    
           :this(currDictionaryParams.GetString("access_token"), currDictionaryParams.GetString("access_token_secret"), currDictionaryParams.GetLong("expires_in"))
        {
	    }
    	
	    public bool IsValid() 
	    {
            return (getAccessToken() != null) && (GSSession.CurrentTimeMillis() < getExpirationTime());
	    }
    	
     }
}
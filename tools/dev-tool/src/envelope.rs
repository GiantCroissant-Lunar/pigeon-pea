use serde::{Deserialize, Serialize};
use serde_json::Value;

#[derive(Serialize, Deserialize)]
pub struct Envelope {
    pub cmd: String,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub args: Option<Value>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub status: Option<String>,
    #[serde(skip_serializing_if = "Option::is_none")]
    pub message: Option<String>,
}

impl Envelope {
    pub fn success(cmd: String, args: Option<Value>) -> Self {
        Self {
            cmd,
            args,
            status: Some("success".to_string()),
            message: None,
        }
    }

    pub fn error(cmd: String, message: String) -> Self {
        Self {
            cmd,
            args: None,
            status: Some("error".to_string()),
            message: Some(message),
        }
    }

    pub fn to_json(&self) -> Result<String, serde_json::Error> {
        serde_json::to_string_pretty(self)
    }
}

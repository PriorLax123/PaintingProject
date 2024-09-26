#include <SoftwareSerial.h>
#include Secrets.h
#define BUFFER_SIZE 100

SoftwareSerial esp(13, 12);
char inputBuffer[BUFFER_SIZE];
int bufferIndex = 0;
int x_val = 110;
const unsigned long TIMEOUT = 10000;

int topVal = 255;
int bottomVal = 110;
int old_val = bottomVal;
int delayInt = 8;
int stepSize = 20;
int ACTUATOR_OUT_PWM = 9;


int times = 0;


void setup() {
  Serial.begin(9600);
  esp.begin(9600);

  setAct();

  if (!runCommand("AT", "OK")) restartModule();
  if (!runCommand("AT+CWMODE=1", "OK")) restartModule();
  if (!runCommand("AT+CWJAP=\"SSD GOES HERE\",\"PASSWORD GOES HERE\"", "OK")) restartModule();
  if (!runCommand("AT+CIPMUX=1", "OK")) restartModule();
  while (true) {
    if (!runCommand("AT+CIPSTART=0,\"TCP\",\"HTTP SERVER GOES HERE\",80", "OK")) restartModule();
    String str = "GET REST OF GET PATH GOES HERE\r\n\n";
    if (!runCommand("AT+CIPSEND=0," + String(str.length() + 1) , "OK")) restartModule();
    if (!runCommand(str + "AT+CIPCLOSE=0", "+IPD")) restartModule();

    // Serial.println("NEW: " + x_val);
    // Serial.println("OLD: " + old_val);
    setAct();
    if(old_val == x_val){
      times++;
    }else{
      old_val = x_val;
      times = 0;
    }

    if (!runCommand("", "CLOSED")) restartModule();

    if( times >= 20 && times < 40 ){
      Serial.println("DELAY 1000");
      delay(1000);
    }else if( times >= 40 && times < 100 ){
      Serial.println("DELAY 5000");
      delay(5000);
    }else if( times >= 100 ){
      Serial.println("DELAY 20000");
      delay(20000);
    }
  }
}

bool runCommand(const String& command, const String& expectedResponse) {
  // Serial.println("Sending command: " + command);
  esp.println(command);

  unsigned long startTime = millis();
  while (millis() - startTime < TIMEOUT) {
    if (esp.available()) {
      String response = "";
      while (esp.available()) {
        response += (char)esp.read();
        delay(1);
      }
      // Serial.println("Received response: " + response);
      if ( response.indexOf(expectedResponse) != -1) {
        if( expectedResponse.indexOf("+IPD") != -1 ){
              parseInput(response);
        }
        return true;
      }
    }
  }

  Serial.println("Command timed out or response incorrect.");
  return false;
}

void restartModule() {
  Serial.println("Restarting module...");
  esp.println("AT+RST");
  delay(5000);
  setup();
}

void parseInput(const String& input) {

  int colonIndex = input.indexOf(':');
  if (colonIndex != -1) {
    String value = input.substring(colonIndex + 1);
    value.trim();

    if (value.length() > 0 && value.toInt() != 0) {
      x_val = value.toInt();
    }else{
      Serial.println("Invalid int value!");
    }
  } else {
    Serial.println("Colon not found in input.");
  }
}

void setAct(){
  int i = old_val;
  if(old_val >= x_val){
    for(;i>x_val ;i-=stepSize){
      analogWrite(ACTUATOR_OUT_PWM, i);
      delay(delayInt);
    }
  }else{
    for(;i<x_val ;i+=stepSize){
      analogWrite(ACTUATOR_OUT_PWM, i);
      delay(delayInt);
    }
  }
  analogWrite(ACTUATOR_OUT_PWM, x_val);
}

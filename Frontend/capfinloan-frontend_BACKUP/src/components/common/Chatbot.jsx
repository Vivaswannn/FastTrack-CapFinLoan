import React, { useState, useRef, useEffect } from 'react';

const SYSTEM_PROMPT = `You are FinBot, a helpful virtual assistant for CapFinLoan,
a financial loan application platform in India.

You help users with:
- Loan application process and steps
- Document requirements: Aadhaar Card, PAN Card, salary slips, bank statements
- Loan types: Personal, Home, Vehicle, Education, Business
- Interest rates: Personal from 10.5% pa, Home from 8.5% pa, Vehicle from 9% pa
- Eligibility: stable employment, monthly income, credit score above 700
- EMI formula: P x r x (1+r)^n divided by (1+r)^n minus 1
- Application statuses: Draft, Submitted, DocsPending, DocsVerified, UnderReview, Approved, Rejected, Closed
- How to upload KYC documents on the Documents page
- Contact support: support@capfinloan.com or toll-free 1800-200-300

Rules:
- Keep all responses short and clear, maximum 3 sentences
- Be polite and professional at all times
- If asked anything unrelated to loans or CapFinLoan, politely say you can only help with loan-related queries
- Never make up specific application IDs or account balances`;

const QUICK_SUGGESTIONS = ['Check Loan Status', 'Interest Rates', 'Document Help'];

const OFFLINE_MESSAGE =
  'FinBot is currently offline. Please contact support@capfinloan.com or call 1800-200-300 for assistance.';

export default function Chatbot() {
  const [isOpen, setIsOpen] = useState(false);
  const [messages, setMessages] = useState([
    { role: 'assistant', text: 'Hello! I am FinBot, your virtual assistant for CapFinLoan. How can I help you today?' }
  ]);
  const [input, setInput] = useState('');
  const [isTyping, setIsTyping] = useState(false);
  const messagesEndRef = useRef(null);
  const inputRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  useEffect(() => {
    if (isOpen) {
      scrollToBottom();
      inputRef.current?.focus();
    }
  }, [messages, isOpen]);

  const sendMessage = async (text) => {
    const trimmed = text.trim();
    if (!trimmed || isTyping) return;

    // Add user message
    const userMessage = { role: 'user', text: trimmed };
    setMessages(prev => [...prev, userMessage]);
    setInput('');
    setIsTyping(true);

    // Build conversation history for Ollama (exclude the initial greeting)
    const history = [...messages.slice(1), userMessage].map(m => ({
      role: m.role === 'assistant' ? 'assistant' : 'user',
      content: m.text
    }));

    try {
      const response = await fetch('http://localhost:11434/api/chat', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          model: 'llama3.2',
          messages: [
            { role: 'system', content: SYSTEM_PROMPT },
            ...history
          ],
          stream: false
        })
      });

      if (!response.ok) throw new Error('Ollama responded with ' + response.status);

      const data = await response.json();
      const botReply = data.message?.content ?? OFFLINE_MESSAGE;

      setMessages(prev => [...prev, { role: 'assistant', text: botReply }]);
    } catch {
      setMessages(prev => [...prev, { role: 'assistant', text: OFFLINE_MESSAGE }]);
    } finally {
      setIsTyping(false);
    }
  };

  const handleSuggestion = (suggestion) => {
    setInput(suggestion);
    sendMessage(suggestion);
  };

  const handleKeyDown = (e) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      sendMessage(input);
    }
  };

  return (
    <>
      {/* Floating Button */}
      {!isOpen && (
        <button
          onClick={() => setIsOpen(true)}
          className="fixed bottom-6 right-6 w-14 h-14 bg-blue-600 text-white rounded-full shadow-lg flex items-center justify-center hover:bg-blue-700 transition-all z-50 animate-bounce"
        >
          <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2"
              d="M8 10h.01M12 10h.01M16 10h.01M21 16.5a2.5 2.5 0 01-2.5 2.5H6l-4 4V5a2.5 2.5 0 012.5-2.5h15A2.5 2.5 0 0121 5v11.5z" />
          </svg>
        </button>
      )}

      {/* Chatbot Window */}
      {isOpen && (
        <div className="fixed bottom-6 right-6 w-80 sm:w-96 bg-white rounded-2xl shadow-2xl flex flex-col overflow-hidden z-50 border border-gray-100 font-sans">

          {/* Header */}
          <div className="bg-gradient-to-r from-blue-700 to-blue-600 p-4 flex justify-between items-center shadow-sm">
            <div className="flex items-center gap-2 text-white">
              <span className="text-xl">🤖</span>
              <div>
                <span className="font-semibold text-lg leading-none">FinBot Assistant</span>
                <p className="text-blue-200 text-xs">Powered by AI · CapFinLoan</p>
              </div>
            </div>
            <button
              onClick={() => setIsOpen(false)}
              className="text-white hover:bg-white/20 p-1.5 rounded-lg transition-colors"
            >
              <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* Messages Area */}
          <div className="flex-1 p-4 bg-gray-50 overflow-y-auto min-h-[300px] max-h-[350px] space-y-4">
            {messages.map((msg, index) => (
              <div key={index} className={`flex ${msg.role === 'user' ? 'justify-end' : 'justify-start'}`}>
                <div
                  className={`max-w-[85%] p-3 rounded-2xl text-sm leading-relaxed shadow-sm whitespace-pre-wrap ${
                    msg.role === 'user'
                      ? 'bg-blue-600 text-white rounded-br-none'
                      : 'bg-white text-gray-800 border border-gray-100 rounded-bl-none'
                  }`}
                >
                  {msg.text}
                </div>
              </div>
            ))}

            {/* Typing indicator */}
            {isTyping && (
              <div className="flex justify-start">
                <div className="bg-white text-gray-800 border border-gray-100 rounded-2xl rounded-bl-none p-3 shadow-sm flex items-center gap-1">
                  <span className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '0ms' }} />
                  <span className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '150ms' }} />
                  <span className="w-2 h-2 bg-blue-400 rounded-full animate-bounce" style={{ animationDelay: '300ms' }} />
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Input Area */}
          <div className="p-3 bg-white border-t border-gray-100">
            <div className="flex gap-2 mb-2">
              <input
                ref={inputRef}
                type="text"
                value={input}
                onChange={e => setInput(e.target.value)}
                onKeyDown={handleKeyDown}
                placeholder="Type your question..."
                disabled={isTyping}
                className="flex-1 text-sm border border-gray-200 rounded-full px-4 py-2 outline-none focus:border-blue-400 focus:ring-1 focus:ring-blue-100 disabled:bg-gray-50 disabled:text-gray-400 transition-colors"
              />
              <button
                onClick={() => sendMessage(input)}
                disabled={!input.trim() || isTyping}
                className="w-9 h-9 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-300 text-white rounded-full flex items-center justify-center transition-colors flex-shrink-0"
              >
                <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M12 19l9 2-9-18-9 18 9-2zm0 0v-8" />
                </svg>
              </button>
            </div>

            {/* Quick Suggestion Buttons */}
            <div className="flex flex-wrap gap-1.5">
              {QUICK_SUGGESTIONS.map((suggestion) => (
                <button
                  key={suggestion}
                  onClick={() => handleSuggestion(suggestion)}
                  disabled={isTyping}
                  className="text-xs font-medium text-blue-600 bg-blue-50 hover:bg-blue-100 disabled:opacity-50 py-1 px-2.5 rounded-full border border-blue-200 transition-colors"
                >
                  {suggestion}
                </button>
              ))}
            </div>
          </div>

        </div>
      )}
    </>
  );
}

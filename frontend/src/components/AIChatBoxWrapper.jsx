import React, { useEffect, useState } from "react";
import AIChatBox from "./AIChatBox";

const AIChatBoxWrapper = ({ showAIChatBox, onClose }) => {
  //Add state to detect if we're on mobile
  const [isMobile, setIsMobile] = useState(window.innerWidth < 768);

  // Update mobile state when window resizes
  useEffect(() => {
    const handleResize = () => {
      setIsMobile(window.innerWidth < 768);
    };

    window.addEventListener("resize", handleResize);
    return () => window.removeEventListener("resize", handleResize);
  }, []);

  // Prevent scrolling when AIchatBox is open on mobile
  useEffect(() => {
    if (showAIChatBox) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [showAIChatBox]);

  const [renderAIChatBox, setRenderAIChatBox] = useState(showAIChatBox);
  const [closing, setClosing] = useState(true);

  useEffect(() => {
    if (showAIChatBox) {
      setRenderAIChatBox(true);
      requestAnimationFrame(() => {
        requestAnimationFrame(() => setClosing(false));
      });
      return;
    }

    setClosing(true);
    const timer = setTimeout(() => setRenderAIChatBox(false), 300);
    return () => clearTimeout(timer);
  }, [showAIChatBox]);

  if (!renderAIChatBox) return null;

  return (
    <div className="fixed inset-0 z-50 lg:relative lg:inset-auto lg:z-auto">
      <div
        className={`absolute inset-0 bg-black/10 bg-opacity-40 transition-opacity duration-300 ease-in-out lg:hidden ${
          closing ? "opacity-0" : "opacity-100"
        }`}
        onClick={onClose}
      />

      <div
        className={`absolute h-full inset-y-0 right-0 w-80 transform transition-transform duration-300 ease-in-out lg:relative lg:inset-auto ${
          closing ? "translate-x-full" : "translate-x-0"
        }`}
      >
        <AIChatBox onClose={onClose} />
      </div>
    </div>
  );
};

export default AIChatBoxWrapper;

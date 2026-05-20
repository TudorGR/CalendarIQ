import React, { useEffect, useState } from "react";
import Sidebar from "./Sidebar";

const SidebarWrapper = ({ showSidebar, onClose }) => {
  const [renderSidebar, setRenderSidebar] = useState(showSidebar);
  const [closing, setClosing] = useState(true);

  useEffect(() => {
    if (showSidebar) {
      setRenderSidebar(true);
      requestAnimationFrame(() => {
        requestAnimationFrame(() => setClosing(false));
      });
      return;
    }

    setClosing(true);
    const timer = setTimeout(() => setRenderSidebar(false), 300);
    return () => clearTimeout(timer);
  }, [showSidebar]);

  //Prevent scrolling when sidebar is open on mobile
  useEffect(() => {
    if (showSidebar) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => {
      document.body.style.overflow = "";
    };
  }, [showSidebar]);

  if (!renderSidebar) return null;

  return (
    <div className="md:hidden fixed inset-0 z-50">
      <div
        className={`absolute inset-0 bg-black/10 bg-opacity-40 transition-opacity duration-300 ease-in-out ${
          closing ? "opacity-0" : "opacity-100"
        }`}
        onClick={onClose}
      />

      <div
        className={`absolute inset-y-0 left-0 w-70 transform transition-transform duration-300 ease-in-out ${
          closing ? "-translate-x-full" : "translate-x-0"
        }`}
      >
        <Sidebar onClose={onClose} />
      </div>
    </div>
  );
};

export default SidebarWrapper;
